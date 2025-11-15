using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BLL.Services.AI
{
    public class CompositeTranscriptionService : ITranscriptionService
    {
        private readonly AzureSpeechTranscriptionService _speech;
        private readonly AzureOpenAITranscriptionService _openai;
        private readonly ILogger<CompositeTranscriptionService> _logger;

        public CompositeTranscriptionService(
            AzureSpeechTranscriptionService speech,
            AzureOpenAITranscriptionService openai,
            ILogger<CompositeTranscriptionService> logger)
        {
            _speech = speech;
            _openai = openai;
            _logger = logger;
        }

        public async Task<string?> TranscribeAsync(byte[] audioBytes, string fileName, string contentType, string? languageCode)
        {
            var ct = (contentType ?? string.Empty).ToLowerInvariant();
            bool isWav = ct.Contains("wav") || fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            _logger.LogInformation("[STT] Start transcription file={File} size={Size} type={Type} lang={Lang} isWav={IsWav} win={Win}", fileName, audioBytes?.Length ?? 0, ct, languageCode ?? "(auto)", isWav, isWindows);

            string? result = null;
            var totalSw = Stopwatch.StartNew();

            // 1. Try Azure Speech first if environment supports (Windows + WAV)
            if (isWindows && isWav)
            {
                var swSpeech = Stopwatch.StartNew();
                try
                {
                    _logger.LogDebug("[STT] Attempt Azure Speech first");
                    var speechText = await _speech.TranscribeAsync(audioBytes, fileName, contentType, languageCode);
                    swSpeech.Stop();
                    if (!string.IsNullOrWhiteSpace(speechText))
                    {
                        _logger.LogInformation("[STT] Azure Speech success in {Ms}ms len={Len}", swSpeech.ElapsedMilliseconds, speechText.Length);
                        result = speechText;
                    }
                    else
                    {
                        _logger.LogWarning("[STT] Azure Speech returned empty in {Ms}ms → fallback Whisper", swSpeech.ElapsedMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    var ms = swSpeech.ElapsedMilliseconds;
                    // Handle GStreamer related error gracefully
                    if (ex.Message.Contains("GSTREAMER", StringComparison.OrdinalIgnoreCase))
                        _logger.LogWarning("[STT] Azure Speech GStreamer missing (elapsed {Ms}ms) → fallback Whisper", ms);
                    else
                        _logger.LogWarning(ex, "[STT] Azure Speech exception after {Ms}ms → fallback Whisper", ms);
                }
            }
            else
            {
                _logger.LogDebug("[STT] Skip Azure Speech (isWav={IsWav}, win={Win}) → Whisper first", isWav, isWindows);
            }

            // 2. Fallback to Whisper if Speech failed or skipped
            if (string.IsNullOrWhiteSpace(result))
            {
                var swWhisper = Stopwatch.StartNew();
                try
                {
                    _logger.LogDebug("[STT] Attempt Whisper fallback");
                    var whisper = await _openai.TranscribeAsync(audioBytes, fileName, contentType, languageCode);
                    swWhisper.Stop();
                    if (!string.IsNullOrWhiteSpace(whisper))
                    {
                        _logger.LogInformation("[STT] Whisper success in {Ms}ms len={Len}", swWhisper.ElapsedMilliseconds, whisper.Length);
                        result = whisper;
                    }
                    else
                    {
                        _logger.LogWarning("[STT] Whisper returned empty in {Ms}ms", swWhisper.ElapsedMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[STT] Whisper exception after {Ms}ms", swWhisper.ElapsedMilliseconds);
                }
            }

            totalSw.Stop();
            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.LogWarning("[STT] Transcription failed total={Ms}ms file={File} type={Type}", totalSw.ElapsedMilliseconds, fileName, ct);
            }
            else
            {
                var provider = (isWindows && isWav && !string.IsNullOrWhiteSpace(result)) ? "AzureSpeechOrWhisper" : "Whisper"; // generic label
                _logger.LogInformation("[STT] Completed provider={Provider} total={Ms}ms", provider, totalSw.ElapsedMilliseconds);
            }
            return result;
        }
    }
}
