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
            //1) Try Azure Speech (will transcode via ffmpeg when available and do in-memory WAV fast path)
            try
            {
                var text = await _speech.TranscribeAsync(audioBytes, fileName, contentType, languageCode);
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Azure Speech STT threw, will consider OpenAI fallback");
            }

            //2) Fallback to Azure OpenAI Whisper for non-WAV or when Speech fails (no GStreamer / ffmpeg)
            try
            {
                var ct = (contentType ?? string.Empty).ToLowerInvariant();
                if (!ct.Contains("wav") || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    _logger.LogInformation("Falling back to Azure OpenAI STT for contentType={ContentType}", contentType);
                    var text = await _openai.TranscribeAsync(audioBytes, fileName, contentType, languageCode);
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure OpenAI STT fallback failed");
            }

            return null;
        }
    }
}
