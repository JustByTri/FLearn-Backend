using BLL.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace BLL.Services.AI
{
    public interface ITranscriptionService
    {
        Task<string?> TranscribeAsync(byte[] audioBytes, string fileName, string contentType, string? languageCode);
    }

    public class AzureOpenAITranscriptionService : ITranscriptionService
    {
        private readonly HttpClient _http;
        private readonly AzureOpenAISettings _settings;
        private readonly ILogger<AzureOpenAITranscriptionService> _logger;

        public AzureOpenAITranscriptionService(HttpClient http, IOptions<AzureOpenAISettings> settings, ILogger<AzureOpenAITranscriptionService> logger)
        {
            _http = http; _settings = settings.Value; _logger = logger;
            if (!string.IsNullOrWhiteSpace(_settings.Endpoint))
            {
                _http.BaseAddress = new Uri(_settings.Endpoint.TrimEnd('/') + "/");
            }
            _http.DefaultRequestHeaders.Remove("api-key");
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                _http.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);
            }
        }

        public async Task<string?> TranscribeAsync(byte[] audioBytes, string fileName, string contentType, string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(_settings.TranscriptionDeployment))
            {
                _logger.LogWarning("AzureOpenAITranscriptionService: TranscriptionDeployment missing -> skip (configure AzureOpenAISettings:TranscriptionDeployment)");
                return null; // disabled
            }
            try
            {
                var deployment = _settings.TranscriptionDeployment.Trim();
                var apiVersion = string.IsNullOrWhiteSpace(_settings.AudioApiVersion) ? _settings.ApiVersion : _settings.AudioApiVersion;
                var url = $"openai/deployments/{deployment}/audio/transcriptions?api-version={apiVersion}";
                _logger.LogDebug("Whisper request: deployment={Deployment}, bytes={Size}, contentType={ContentType}, languageRaw={Lang}", deployment, audioBytes?.Length ?? 0, contentType, languageCode);

                // Normalize language to ISO-639 (Azure OpenAI expects short code for whisper)
                string? normalizedLang = null;
                if (!string.IsNullOrWhiteSpace(languageCode))
                {
                    var lc = languageCode.Trim().ToLowerInvariant();
                    if (lc.StartsWith("en")) normalizedLang = "en";
                    else if (lc.StartsWith("ja")) normalizedLang = "ja";
                    else if (lc.StartsWith("zh")) normalizedLang = "zh"; // covers zh-CN / zh-TW
                    else if (lc.Length == 2) normalizedLang = lc;
                }

                using var form = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(audioBytes);
                var ct = string.IsNullOrWhiteSpace(contentType) ? "audio/wav" : contentType;
                if (ct.Equals("audio/x-wav", StringComparison.OrdinalIgnoreCase)) ct = "audio/wav";
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(ct);
                form.Add(fileContent, "file", string.IsNullOrWhiteSpace(fileName) ? "audio.wav" : fileName);
                if (!string.IsNullOrWhiteSpace(normalizedLang))
                {
                    form.Add(new StringContent(normalizedLang), "language");
                    _logger.LogDebug("Added language param: {Lang}", normalizedLang);
                }

                var res = await _http.PostAsync(url, form);
                var txt = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Whisper transcription failed Status={Status} Body={BodySnippet}", (int)res.StatusCode, txt.Length > 500 ? txt.Substring(0, 500) : txt);
                    return null;
                }
                _logger.LogDebug("Whisper raw response length={Len}", txt.Length);
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(txt);
                    if (doc.RootElement.TryGetProperty("text", out var t))
                    {
                        var value = t.GetString();
                        _logger.LogInformation("Whisper transcription success. Characters={Chars}", value?.Length ?? 0);
                        return value;
                    }
                    _logger.LogWarning("Whisper response missing 'text' property. Raw={Raw}", txt.Length > 400 ? txt.Substring(0, 400) : txt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse Whisper transcription JSON");
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Whisper transcription exception");
                return null;
            }
        }
    }
}
