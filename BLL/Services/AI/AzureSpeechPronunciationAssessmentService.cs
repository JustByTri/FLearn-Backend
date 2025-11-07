using BLL.IServices.AI;
using BLL.Settings;
using Common.DTO.Assement;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BLL.Services.AI
{
    public class AzureSpeechPronunciationAssessmentService : IPronunciationAssessmentService
    {
        private readonly SpeechSettings _settings;
        private readonly ILogger<AzureSpeechPronunciationAssessmentService> _logger;

        public AzureSpeechPronunciationAssessmentService(IOptions<SpeechSettings> settings, ILogger<AzureSpeechPronunciationAssessmentService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<PronunciationAssessmentResultDto> AssessAsync(byte[] audioBytes, string contentType, string referenceText, string languageCode)
        {
            var config = SpeechConfig.FromSubscription(_settings.ApiKey, _settings.Region);
            config.SpeechRecognitionLanguage = languageCode;
            var paConfig = new PronunciationAssessmentConfig(referenceText,
            GradingSystem.HundredMark,
            Granularity.Phoneme,
            enableMiscue: true);

            // Determine compressed format
            AudioStreamContainerFormat container = AudioStreamContainerFormat.ANY;
            var ct = (contentType ?? string.Empty).ToLowerInvariant();
            if (ct.Contains("wav")) container = AudioStreamContainerFormat.ANY; // auto
            else if (ct.Contains("mp3") || ct.Contains("mpeg")) container = AudioStreamContainerFormat.MP3;
            else if (ct.Contains("ogg") || ct.Contains("webm")) container = AudioStreamContainerFormat.OGG_OPUS;

            var format = AudioStreamFormat.GetCompressedFormat(container);
            using var push = AudioInputStream.CreatePushStream(format);
            push.Write(audioBytes);
            push.Close();
            using var audioCfg = AudioConfig.FromStreamInput(push);
            using var reco = new SpeechRecognizer(config, audioCfg);
            paConfig.ApplyTo(reco);

            var result = await reco.RecognizeOnceAsync();
            var rawJson = result?.Properties?.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
            if (result.Reason != ResultReason.RecognizedSpeech)
            {
                return new PronunciationAssessmentResultDto { RawJson = rawJson };
            }
            var pa = PronunciationAssessmentResult.FromResult(result);
            var dto = new PronunciationAssessmentResultDto
            {
                Accuracy = pa.AccuracyScore,
                Fluency = pa.FluencyScore,
                Completeness = pa.CompletenessScore,
                Pronunciation = pa.PronunciationScore,
                RawJson = rawJson
            };

            try
            {
                if (!string.IsNullOrWhiteSpace(rawJson))
                {
                    var words = ParseWordsFromJson(rawJson);
                    foreach (var w in words)
                    {
                        dto.Words.Add(w);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Cannot parse word-level scores from JSON");
            }

            return dto;
        }

        private static List<WordPronunciationScoreDto> ParseWordsFromJson(string json)
        {
            var list = new List<WordPronunciationScoreDto>();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("NBest", out var nbest) || nbest.ValueKind != JsonValueKind.Array || nbest.GetArrayLength() == 0)
                return list;
            var first = nbest[0];
            if (!first.TryGetProperty("Words", out var wordsEl) || wordsEl.ValueKind != JsonValueKind.Array)
                return list;
            foreach (var w in wordsEl.EnumerateArray())
            {
                var wordText = w.TryGetProperty("Word", out var we) ? we.GetString() : null;
                double acc = 0;
                if (w.TryGetProperty("Pronunciation", out var pron) && pron.ValueKind == JsonValueKind.Object)
                {
                    if (pron.TryGetProperty("AccuracyScore", out var accEl) && accEl.TryGetDouble(out var d)) acc = d;
                }
                var errorType = w.TryGetProperty("ErrorType", out var e) ? e.GetString() : null;
                list.Add(new WordPronunciationScoreDto
                {
                    Word = wordText ?? string.Empty,
                    Accuracy = acc,
                    IsInserted = string.Equals(errorType, "Inserted", StringComparison.OrdinalIgnoreCase),
                    IsOmitted = string.Equals(errorType, "Omitted", StringComparison.OrdinalIgnoreCase),
                    IsSubstituted = string.Equals(errorType, "Substituted", StringComparison.OrdinalIgnoreCase)
                });
            }
            return list;
        }
    }
}
