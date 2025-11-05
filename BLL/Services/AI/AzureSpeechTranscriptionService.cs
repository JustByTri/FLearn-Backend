using BLL.Settings;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BLL.Services.AI
{
 public class AzureSpeechTranscriptionService : ITranscriptionService
 {
 private readonly SpeechSettings _settings;
 private readonly ILogger<AzureSpeechTranscriptionService> _logger;

 public AzureSpeechTranscriptionService(IOptions<SpeechSettings> settings, ILogger<AzureSpeechTranscriptionService> logger)
 {
 _settings = settings.Value; _logger = logger;
 }

 public async Task<string?> TranscribeAsync(byte[] audioBytes, string fileName, string contentType, string? languageCode)
 {
 try
 {
 var config = SpeechConfig.FromSubscription(_settings.ApiKey, _settings.Region);
 // If a locale is provided, prefer explicit recognition; else use auto-detect across configured languages.
 var langs = _settings.Languages?.ToArray() ?? new[] { "en-US", "ja-JP", "zh-CN" };
 AutoDetectSourceLanguageConfig? autoCfg = null;
 if (string.IsNullOrWhiteSpace(languageCode))
 {
 autoCfg = AutoDetectSourceLanguageConfig.FromLanguages(langs);
 }
 else
 {
 config.SpeechRecognitionLanguage = languageCode!;
 }

 // Determine by extension/content type
 string? ext = null;
 if (!string.IsNullOrWhiteSpace(fileName)) ext = Path.GetExtension(fileName).ToLowerInvariant();
 var ct = (contentType ?? string.Empty).ToLowerInvariant();

 // Prefer WAV file input (PCM) when applicable
 if (ct.Contains("wav") || ext == ".wav")
 {
 var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".wav");
 await File.WriteAllBytesAsync(tmp, audioBytes);
 try
 {
 using var audioConfig = AudioConfig.FromWavFileInput(tmp);
 var text = await RecognizeOnceAsync(config, audioConfig, autoCfg);
 if (!string.IsNullOrWhiteSpace(text)) return text;

 // Fallback: if auto failed, force en-US
 if (autoCfg != null)
 {
 config.SpeechRecognitionLanguage = "en-US";
 using var forcedAudioCfg = AudioConfig.FromWavFileInput(tmp);
 var forcedText = await RecognizeOnceAsync(config, forcedAudioCfg, null);
 if (!string.IsNullOrWhiteSpace(forcedText)) return forcedText;
 }
 return null;
 }
 finally { try { File.Delete(tmp); } catch { } }
 }

 // Compressed stream: support MP3 and OGG/WEBM (map to OGG_OPUS for SDKs without WEBM_OPUS)
 AudioStreamContainerFormat container = AudioStreamContainerFormat.ANY;
 if (ct.Contains("mp3") || ext == ".mp3" || ct.Contains("mpeg"))
 {
 container = AudioStreamContainerFormat.MP3;
 }
 else if (ct.Contains("ogg") || ct.Contains("webm") || ext == ".ogg" || ext == ".webm")
 {
 container = AudioStreamContainerFormat.OGG_OPUS;
 }

 var format = AudioStreamFormat.GetCompressedFormat(container);
 using (var push = AudioInputStream.CreatePushStream(format))
 {
 push.Write(audioBytes);
 push.Close();
 using var audioConfig = AudioConfig.FromStreamInput(push);
 var text = await RecognizeOnceAsync(config, audioConfig, autoCfg);
 if (!string.IsNullOrWhiteSpace(text)) return text;

 if (autoCfg != null)
 {
 config.SpeechRecognitionLanguage = "en-US";
 // Rebuild stream for retry
 using var push2 = AudioInputStream.CreatePushStream(format);
 push2.Write(audioBytes);
 push2.Close();
 using var audioConfig2 = AudioConfig.FromStreamInput(push2);
 var forcedText = await RecognizeOnceAsync(config, audioConfig2, null);
 if (!string.IsNullOrWhiteSpace(forcedText)) return forcedText;
 }
 }
 return null;
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Azure Speech transcription error");
 return null;
 }
 }

 private async Task<string?> RecognizeOnceAsync(SpeechConfig config, AudioConfig audioConfig, AutoDetectSourceLanguageConfig? autoCfg)
 {
 SpeechRecognitionResult result;
 if (autoCfg != null)
 {
 using var recognizer = new SourceLanguageRecognizer(config, autoCfg, audioConfig);
 result = await recognizer.RecognizeOnceAsync();
 var detected = result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);
 _logger.LogDebug("STT Auto locale: {Locale}", detected);
 }
 else
 {
 using var recognizer = new SpeechRecognizer(config, audioConfig);
 result = await recognizer.RecognizeOnceAsync();
 }

 if (result.Reason == ResultReason.RecognizedSpeech)
 {
 return result.Text;
 }
 if (result.Reason == ResultReason.Canceled)
 {
 var details = CancellationDetails.FromResult(result);
 _logger.LogWarning("STT canceled. Code={Code}, Reason={Reason}, Error={Error}", details.ErrorCode, details.Reason, details.ErrorDetails);
 }
 else
 {
 _logger.LogWarning("STT no match. Reason={Reason}", result.Reason);
 }
 return null;
 }
 }
}
