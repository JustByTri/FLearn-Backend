using Microsoft.Extensions.Logging;
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

            // 1. Always try Azure OpenAI Whisper first (robust for all formats, no GStreamer).
            try
            {
                var whisper = await _openai.TranscribeAsync(audioBytes, fileName, contentType, languageCode);
                if (!string.IsNullOrWhiteSpace(whisper))
                {
                    _logger.LogDebug("Transcription succeeded via Azure OpenAI Whisper");
                    return whisper;
                }
                _logger.LogInformation("Azure OpenAI Whisper returned null/empty, will attempt Azure Speech fallback");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure OpenAI Whisper threw, attempting Azure Speech fallback");
            }

            // 2. Fallback: Azure Speech ONLY for WAV & Windows (to avoid GStreamer issues on Linux/macOS).
            if (isWav && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var speechText = await _speech.TranscribeAsync(audioBytes, fileName, contentType, languageCode);
                    if (!string.IsNullOrWhiteSpace(speechText))
                    {
                        _logger.LogDebug("Transcription succeeded via Azure Speech fallback");
                        return speechText;
                    }
                    _logger.LogInformation("Azure Speech fallback returned empty");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Azure Speech fallback failed");
                }
            }
            else
            {
                _logger.LogDebug("Skipping Azure Speech fallback (isWav={IsWav}, Platform={Platform})", isWav, RuntimeInformation.OSDescription);
            }

            _logger.LogWarning("All transcription paths failed (file={FileName}, contentType={ContentType})", fileName, contentType);
            return null;
        }
    }
}
