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
            if (string.IsNullOrWhiteSpace(_settings.TranscriptionDeployment)) return null; // disabled
            var url = $"openai/deployments/{_settings.TranscriptionDeployment}/audio/transcriptions?api-version={_settings.ApiVersion}";
            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(audioBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "audio/m4a" : contentType);
            form.Add(fileContent, "file", fileName);
            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                form.Add(new StringContent(languageCode!), "language");
            }

            var res = await _http.PostAsync(url, form);
            var txt = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Transcription failed {Status}: {Text}", res.StatusCode, txt);
                return null;
            }
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(txt);
                return doc.RootElement.TryGetProperty("text", out var t) ? t.GetString() : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
