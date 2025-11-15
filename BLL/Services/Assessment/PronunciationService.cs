using BLL.IServices.Assessment;
using Common.DTO.ExerciseGrading.Response;
using Common.DTO.Pronunciation;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BLL.Services.Assessment
{
    public class PronunciationService : IPronunciationService
    {
        private readonly IConfiguration _configuration;
        public PronunciationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<Common.DTO.Pronunciation.PronunciationAssessmentResult> AssessPronunciationAsync(string audioUrl, string referenceText, string languageCode = "en")
        {
            try
            {
                var lang = MapLanguage(languageCode);

                var speechConfig = SpeechConfig.FromSubscription(
                    _configuration["SpeechSettings:ApiKey"],
                    _configuration["SpeechSettings:Region"]);

                speechConfig.SpeechRecognitionLanguage = lang;

                var paConfig = new PronunciationAssessmentConfig(
                    referenceText,
                    GradingSystem.HundredMark,
                    Granularity.Phoneme,
                    enableMiscue: true);

                using var http = new HttpClient();
                var audioBytes = await http.GetByteArrayAsync(audioUrl);

                var tempFilePath = Path.GetTempFileName() + ".wav";
                await File.WriteAllBytesAsync(tempFilePath, audioBytes);

                using var audioConfig = AudioConfig.FromWavFileInput(tempFilePath);
                using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

                paConfig.ApplyTo(recognizer);

                var result = await recognizer.RecognizeOnceAsync();

                try { File.Delete(tempFilePath); } catch { }

                if (result.Reason != ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"Recognition failed. Reason: {result.Reason}");
                    return null;
                }

                return ParsePronunciationResult(result);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error: {ex.Message}");
            }
        }
        public AssessmentResult ConvertToAssessmentResult(Common.DTO.Pronunciation.PronunciationAssessmentResult azureResult, string referenceText, string languageCode)
        {
            var overallScore = (int)azureResult.PronunciationScore;

            var feedback = new StringBuilder();

            feedback.AppendLine($"**Pronunciation Assessment Results**");
            feedback.AppendLine($"Overall Pronunciation: {azureResult.PronunciationScore:F1}%");
            feedback.AppendLine($"Accuracy: {azureResult.AccuracyScore:F1}%");
            feedback.AppendLine($"Fluency: {azureResult.FluencyScore:F1}%");
            feedback.AppendLine($"Completeness: {azureResult.CompletenessScore:F1}%");

            if (azureResult.PhonemeAssessments?.Any() == true)
            {
                var poorPhonemes = azureResult.PhonemeAssessments
                    .Where(p => p.AccuracyScore < 70)
                    .OrderBy(p => p.AccuracyScore)
                    .Take(3)
                    .Select(p => $"{p.Phoneme} ({p.AccuracyScore:F0}%)");

                if (poorPhonemes.Any())
                {
                    feedback.AppendLine($"\n**Areas for Improvement**: {string.Join(", ", poorPhonemes)}");
                }

                var excellentPhonemes = azureResult.PhonemeAssessments
                    .Where(p => p.AccuracyScore >= 90)
                    .Take(3)
                    .Select(p => p.Phoneme);

                if (excellentPhonemes.Any())
                {
                    feedback.AppendLine($"**Excellent Pronunciation**: {string.Join(", ", excellentPhonemes)}");
                }
            }

            feedback.AppendLine($"\n**Reference Text**: {referenceText}");
            feedback.AppendLine($"**Tip**: Practice speaking slowly and focus on problem sounds identified above.");

            return new AssessmentResult
            {
                Scores = new ExtendedScores
                {
                    Pronunciation = (int)azureResult.PronunciationScore,
                    Fluency = (int)azureResult.FluencyScore,
                    Coherence = (int)((azureResult.AccuracyScore + azureResult.CompletenessScore) / 2),
                    Accuracy = (int)azureResult.AccuracyScore,
                    Intonation = (int)((azureResult.FluencyScore + azureResult.PronunciationScore) / 2),
                    Grammar = 0,
                    Vocabulary = 0
                },
                CefrLevel = GetEnLevel(overallScore),
                JlptLevel = languageCode == "ja" ? GetJlptLevel(overallScore) : null,
                HskLevel = languageCode == "zh" ? GetHskLevel(overallScore) : null,
                Overall = overallScore,
                Feedback = feedback.ToString(),
                Transcript = referenceText
            };
        }
        #region
        private Common.DTO.Pronunciation.PronunciationAssessmentResult ParsePronunciationResult(SpeechRecognitionResult result)
        {
            try
            {
                if (result.Reason != ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"**[FAILED]**Speech recognition failed: {result.Reason}");
                    return null;
                }

                var jsonText = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
                using var jsonDoc = JsonDocument.Parse(jsonText);

                if (!jsonDoc.RootElement.TryGetProperty("NBest", out var nBestElement) ||
                    nBestElement.GetArrayLength() == 0)
                {
                    Console.WriteLine("**[FAILED]**NBest array not found or empty in JSON");
                    return null;
                }

                var firstNBest = nBestElement[0];

                if (!firstNBest.TryGetProperty("PronunciationAssessment", out var pronunciationAssessment))
                {
                    Console.WriteLine("**[FAILED]**PronunciationAssessment property not found in NBest");
                    return null;
                }

                var assessmentResult = new Common.DTO.Pronunciation.PronunciationAssessmentResult
                {
                    AccuracyScore = pronunciationAssessment.GetProperty("AccuracyScore").GetSingle(),
                    FluencyScore = pronunciationAssessment.GetProperty("FluencyScore").GetSingle(),
                    CompletenessScore = pronunciationAssessment.GetProperty("CompletenessScore").GetSingle(),
                    PronunciationScore = pronunciationAssessment.GetProperty("PronScore").GetSingle()
                };

                if (firstNBest.TryGetProperty("Words", out var wordsElement))
                {
                    assessmentResult.PhonemeAssessments = ParsePhonemeDetails(firstNBest);
                }

                Console.WriteLine($"**[OK]**Parsed Successfully - Acc: {assessmentResult.AccuracyScore}, Flu: {assessmentResult.FluencyScore}, Comp: {assessmentResult.CompletenessScore}, Pron: {assessmentResult.PronunciationScore}");

                return assessmentResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"**[FAILED]**Error parsing pronunciation result: {ex.Message}");
                return null;
            }
        }
        private List<PhonemeAssessment> ParsePhonemeDetails(JsonElement nBestElement)
        {
            var phonemeAssessments = new List<PhonemeAssessment>();

            try
            {
                if (nBestElement.TryGetProperty("Words", out var wordsElement))
                {
                    foreach (var wordElement in wordsElement.EnumerateArray())
                    {
                        if (wordElement.TryGetProperty("Phonemes", out var phonemesElement))
                        {
                            foreach (var phonemeElement in phonemesElement.EnumerateArray())
                            {
                                var phonemeAssessment = new PhonemeAssessment
                                {
                                    Phoneme = phonemeElement.GetProperty("Phoneme").GetString() ?? string.Empty,
                                    AccuracyScore = phonemeElement.GetProperty("PronunciationAssessment").GetProperty("AccuracyScore").GetSingle(),
                                    Offset = (int)phonemeElement.GetProperty("Offset").GetInt64(),
                                    Duration = (int)phonemeElement.GetProperty("Duration").GetInt64()
                                };
                                phonemeAssessments.Add(phonemeAssessment);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing phoneme details: {ex.Message}");
            }

            return phonemeAssessments;
        }
        private string MapLanguage(string languageCode)
        {
            return languageCode switch
            {
                "en" => "en-US",
                "ja" => "ja-JP",
                "zh" => "zh-CN",
                _ => "en-US"
            };
        }
        private string GetEnLevel(int score) => score >= 90 ? "C2" : score >= 80 ? "C1" : score >= 70 ? "B2" : score >= 60 ? "B1" : score >= 50 ? "A2" : "A1";
        private string GetJlptLevel(int score) => score >= 90 ? "N1" : score >= 80 ? "N2" : score >= 70 ? "N3" : score >= 60 ? "N4" : "N5";
        private string GetHskLevel(int score) => score >= 90 ? "HSK6" : score >= 80 ? "HSK5" : score >= 70 ? "HSK4" : score >= 60 ? "HSK3" : score >= 50 ? "HSK2" : "HSK1";
        #endregion
    }
}
