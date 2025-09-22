using BLL.IServices.AI;
using BLL.Settings;
using Common.DTO.Learner;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BLL.Services.AI
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiSettings _settings;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(HttpClient httpClient, IOptions<GeminiSettings> settings, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<AiCourseRecommendationDto> GenerateCourseRecommendationsAsync(
            UserSurveyResponseDto survey,
            List<CourseInfoDto> availableCourses)
        {
            try
            {
                var prompt = BuildCourseRecommendationPrompt(survey, availableCourses);
                var response = await CallGeminiApiAsync(prompt);

                return ParseCourseRecommendationResponse(response, availableCourses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating course recommendations for user survey {SurveyId}", survey.SurveyID);

                // Return fallback response instead of throwing
                return new AiCourseRecommendationDto
                {
                    RecommendedCourses = new List<CourseRecommendationDto>(),
                    ReasoningExplanation = "Hiện tại không thể tạo gợi ý từ AI. Vui lòng thử lại sau.",
                    LearningPath = "Bạn có thể bắt đầu với các khóa học cơ bản phù hợp với trình độ của mình.",
                    StudyTips = new List<string> { "Học đều đặn mỗi ngày", "Luyện tập thường xuyên", "Tìm kiếm tài liệu phù hợp" },
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<string> GenerateStudyPlanAsync(UserSurveyResponseDto survey)
        {
            try
            {
                var prompt = BuildStudyPlanPrompt(survey);
                return await CallGeminiApiAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating study plan for user survey {SurveyId}", survey.SurveyID);
                return "Hiện tại không thể tạo kế hoạch học tập từ AI. Vui lòng thử lại sau.";
            }
        }

        public async Task<List<string>> GenerateStudyTipsAsync(UserSurveyResponseDto survey)
        {
            try
            {
                var prompt = BuildStudyTipsPrompt(survey);
                var response = await CallGeminiApiAsync(prompt);

                return ParseStudyTipsResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating study tips for user survey {SurveyId}", survey.SurveyID);

                // Return fallback tips
                return new List<string>
                {
                    "Học đều đặn mỗi ngày, dù chỉ 15-30 phút",
                    "Luyện tập các kỹ năng nghe, nói, đọc, viết một cách cân bằng",
                    "Sử dụng flashcard để ghi nhớ từ vựng mới",
                    "Xem phim, nghe nhạc bằng ngôn ngữ đang học",
                    "Tìm partner để luyện tập hội thoại"
                };
            }
        }

        private string BuildCourseRecommendationPrompt(UserSurveyResponseDto survey, List<CourseInfoDto> courses)
        {
            var coursesJson = JsonSerializer.Serialize(courses, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            return $@"# Phân tích và gợi ý khóa học phù hợp

## Thông tin người học:

- **Trình độ hiện tại**: {survey.CurrentLevel}
- **Ngôn ngữ muốn học**: {survey.PreferredLanguageName}
- **Lý do học**: {survey.LearningReason}
- **Kinh nghiệm trước đó**: {survey.PreviousExperience}
- **Phong cách học ưa thích**: {survey.PreferredLearningStyle}
- **Chủ đề quan tâm**: {survey.InterestedTopics}
- **Kỹ năng ưu tiên**: {survey.PrioritySkills}
- **Thời hạn mục tiêu**: {survey.TargetTimeline}

## Danh sách khóa học có sẵn:
{coursesJson}

## Yêu cầu:
Hãy phân tích thông tin người học và đề xuất 3-5 khóa học phù hợp nhất từ danh sách trên.

Trả về kết quả CHÍNH XÁC theo định dạng JSON như sau (không được thêm markdown hay ký tự khác):

{{
    ""recommendedCourses"": [
        {{
            ""courseId"": ""guid-của-khóa-học"",
            ""matchScore"": 95,
            ""matchReason"": ""Lý do cụ thể tại sao khóa học này phù hợp""
        }}
    ],
    ""reasoningExplanation"": ""Giải thích tổng quan về việc lựa chọn"",
    ""learningPath"": ""Lộ trình học tập được đề xuất chi tiết"",
    ""studyTips"": [
        ""Mẹo học tập cụ thể 1"",
        ""Mẹo học tập cụ thể 2"",
        ""Mẹo học tập cụ thể 3""
    ]
}}

Hãy trả lời bằng tiếng Việt và tập trung vào việc match đúng trình độ, mục tiêu và sở thích của người học.";
        }

        private string BuildStudyPlanPrompt(UserSurveyResponseDto survey)
        {
            return $@"Dựa trên thông tin học viên, hãy tạo một kế hoạch học tập chi tiết:

**Thông tin học viên:**
- Trình độ: {survey.CurrentLevel}
- Ngôn ngữ: {survey.PreferredLanguageName}
- Thời hạn: {survey.TargetTimeline}
- Kỹ năng ưu tiên: {survey.PrioritySkills}

Hãy tạo kế hoạch học tập theo tuần, bao gồm:
1. Mục tiêu từng tuần
2. Hoạt động học tập hàng ngày
3. Đánh giá tiến độ
4. Lời khuyên cụ thể

Trả lời bằng tiếng Việt, chi tiết và thực tế.";
        }

        private string BuildStudyTipsPrompt(UserSurveyResponseDto survey)
        {
            return $@"Dựa trên thông tin sau của học viên:
- Phong cách học: {survey.PreferredLearningStyle}
- Trình độ: {survey.CurrentLevel}
Hãy đưa ra 8-10 mẹo học tập cụ thể và thực tế.

Trả về danh sách mẹo, mỗi mẹo trên một dòng, bắt đầu bằng ""- "".
Viết bằng tiếng Việt, ngắn gọn nhưng hữu ích.";
        }

        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = _settings.Temperature,
                        maxOutputTokens = _settings.MaxTokens,
                        topP = 0.8,
                        topK = 10
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var url = $"{_settings.BaseUrl}/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

                _logger.LogInformation("Calling Gemini API: {Url}", url);

                var response = await _httpClient.PostAsync(url, content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    throw new HttpRequestException($"Gemini API returned {response.StatusCode}: {responseContent}");
                }

                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var result = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";

                _logger.LogInformation("Gemini API response received: {Length} characters", result.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                throw;
            }
        }

        private AiCourseRecommendationDto ParseCourseRecommendationResponse(string response, List<CourseInfoDto> availableCourses)
        {
            try
            {
                _logger.LogDebug("Parsing AI response: {Response}", response);

                // Clean the response - remove markdown code blocks if present
                var cleanedResponse = response.Trim();

                // Handle different markdown formats
                if (cleanedResponse.StartsWith("```json"))
                {
                    cleanedResponse = cleanedResponse.Replace("```json", "").Replace("```", "").Trim();
                }
                else if (cleanedResponse.StartsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Replace("```", "").Trim();
                }

                // Remove any other markdown artifacts
                cleanedResponse = cleanedResponse.Replace("**", "").Replace("*", "");

                // Find JSON boundaries
                var jsonStart = cleanedResponse.IndexOf('{');
                var jsonEnd = cleanedResponse.LastIndexOf('}') + 1;

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonResponse = cleanedResponse.Substring(jsonStart, jsonEnd - jsonStart);

                    _logger.LogDebug("Extracted JSON: {Json}", jsonResponse);

                    var aiResponse = JsonSerializer.Deserialize<AiResponseFormat>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    });

                    if (aiResponse != null)
                    {
                        var recommendations = new List<CourseRecommendationDto>();

                        foreach (var rec in aiResponse.RecommendedCourses ?? new List<CourseMatch>())
                        {
                            // Try to find course by exact GUID match first
                            var course = availableCourses.FirstOrDefault(c =>
                                c.CourseID.ToString().Equals(rec.CourseId, StringComparison.OrdinalIgnoreCase));

                            // If not found by GUID, try partial match or fuzzy search
                            if (course == null && !string.IsNullOrEmpty(rec.CourseId))
                            {
                                course = availableCourses.FirstOrDefault(c =>
                                    c.Title.Contains(rec.CourseId, StringComparison.OrdinalIgnoreCase) ||
                                    c.CourseID.ToString().Contains(rec.CourseId));
                            }

                            if (course != null)
                            {
                                recommendations.Add(new CourseRecommendationDto
                                {
                                    CourseID = course.CourseID,
                                    CourseName = course.Title,
                                    CourseDescription = course.Description,
                                    Level = course.Level,
                                    MatchScore = Math.Min(100, Math.Max(0, rec.MatchScore)), // Ensure 0-100 range
                                    MatchReason = rec.MatchReason ?? "Phù hợp với mục tiêu học tập",
                                    EstimatedDuration = course.Duration,
                                    Skills = course.Skills ?? new List<string>()
                                });

                                _logger.LogDebug("Matched course: {CourseId} - {Title}", course.CourseID, course.Title);
                            }
                            else
                            {
                                _logger.LogWarning("Course not found for ID: {CourseId}", rec.CourseId);

                                // Create a fallback recommendation if we have course info
                                if (!string.IsNullOrEmpty(rec.CourseId) && availableCourses.Any())
                                {
                                    var fallbackCourse = availableCourses.First();
                                    recommendations.Add(new CourseRecommendationDto
                                    {
                                        CourseID = fallbackCourse.CourseID,
                                        CourseName = fallbackCourse.Title,
                                        CourseDescription = fallbackCourse.Description,
                                        Level = fallbackCourse.Level,
                                        MatchScore = 70,
                                        MatchReason = "Gợi ý thay thế phù hợp",
                                        EstimatedDuration = fallbackCourse.Duration,
                                        Skills = fallbackCourse.Skills ?? new List<string>()
                                    });
                                }
                            }
                        }

                        return new AiCourseRecommendationDto
                        {
                            RecommendedCourses = recommendations,
                            ReasoningExplanation = SanitizeText(aiResponse.ReasoningExplanation) ??
                                                 "AI đã phân tích thông tin của bạn để đưa ra những gợi ý phù hợp nhất.",
                            LearningPath = SanitizeText(aiResponse.LearningPath) ??
                                         "Hãy bắt đầu với khóa học cơ bản và tiến dần lên các cấp độ cao hơn.",
                            StudyTips = SanitizeStudyTips(aiResponse.StudyTips) ?? GetDefaultStudyTips(),
                            GeneratedAt = DateTime.UtcNow
                        };
                    }
                }
                else
                {
                    _logger.LogWarning("No valid JSON found in AI response");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error. Response: {Response}", response.Substring(0, Math.Min(500, response.Length)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing AI response: {Error}", ex.Message);
            }

            // Return fallback response
            return CreateFallbackRecommendation(availableCourses);
        }

        // Helper method to sanitize text from AI
        private string? SanitizeText(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            // Remove any remaining markdown
            var sanitized = text.Replace("**", "")
                               .Replace("*", "")
                               .Replace("#", "")
                               .Trim();

            // Ensure minimum length
            return sanitized.Length > 10 ? sanitized : null;
        }

        // Helper method to sanitize study tips
        private List<string>? SanitizeStudyTips(List<string>? tips)
        {
            if (tips == null || !tips.Any())
                return null;

            var sanitizedTips = new List<string>();

            foreach (var tip in tips)
            {
                var sanitized = SanitizeText(tip);
                if (!string.IsNullOrEmpty(sanitized) && sanitized.Length > 5)
                {
                    sanitizedTips.Add(sanitized);
                }
            }

            return sanitizedTips.Any() ? sanitizedTips : null;
        }

        // Helper method to get default study tips
        private List<string> GetDefaultStudyTips()
        {
            return new List<string>
    {
        "Học đều đặn mỗi ngày, dù chỉ 15-30 phút",
        "Luyện tập các kỹ năng nghe, nói, đọc, viết một cách cân bằng",
        "Sử dụng flashcard để ghi nhớ từ vựng mới",
        "Xem phim, nghe nhạc bằng ngôn ngữ đang học",
        "Tìm partner để luyện tập hội thoại",
        "Đặt mục tiêu học tập rõ ràng cho từng tuần",
        "Ôn tập kiến thức cũ thường xuyên"
    };
        }

        // Helper method to create fallback recommendation
        private AiCourseRecommendationDto CreateFallbackRecommendation(List<CourseInfoDto> availableCourses)
        {
            var fallbackRecommendations = new List<CourseRecommendationDto>();

            // If we have available courses, recommend the first few
            if (availableCourses.Any())
            {
                var topCourses = availableCourses.Take(3);
                foreach (var course in topCourses)
                {
                    fallbackRecommendations.Add(new CourseRecommendationDto
                    {
                        CourseID = course.CourseID,
                        CourseName = course.Title,
                        CourseDescription = course.Description,
                        Level = course.Level,
                        MatchScore = 75,
                        MatchReason = "Khóa học phổ biến phù hợp cho người mới bắt đầu",
                        EstimatedDuration = course.Duration,
                        Skills = course.Skills ?? new List<string>()
                    });
                }
            }

            return new AiCourseRecommendationDto
            {
                RecommendedCourses = fallbackRecommendations,
                ReasoningExplanation = "Hiện tại không thể phân tích được phản hồi từ AI. " +
                                     "Dưới đây là những khóa học phổ biến có thể phù hợp với bạn.",
                LearningPath = "Hãy bắt đầu với khóa học cơ bản phù hợp với trình độ hiện tại của bạn. " +
                              "Sau đó dần dần nâng cao theo từng cấp độ.",
                StudyTips = GetDefaultStudyTips(),
                GeneratedAt = DateTime.UtcNow
            };
        }

        private List<string> ParseStudyTipsResponse(string response)
        {
            var tips = new List<string>();
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Handle bullet points
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("• ") || trimmed.StartsWith("* "))
                {
                    tips.Add(trimmed.Substring(2).Trim());
                }
                // Handle numbered lists
                else if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+\.\s"))
                {
                    var dotIndex = trimmed.IndexOf('.');
                    if (dotIndex > 0 && dotIndex < trimmed.Length - 1)
                    {
                        tips.Add(trimmed.Substring(dotIndex + 1).Trim());
                    }
                }
                // Handle regular sentences that look like tips
                else if (trimmed.Length > 10 &&
                         !trimmed.Contains("mẹo", StringComparison.OrdinalIgnoreCase) &&
                         !trimmed.Contains("sau đây", StringComparison.OrdinalIgnoreCase) &&
                         !trimmed.Contains("dưới đây", StringComparison.OrdinalIgnoreCase))
                {
                    tips.Add(trimmed);
                }
            }

            return tips.Take(10).ToList();
        }

        #region Helper Classes for JSON Deserialization

        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<Candidate>? Candidates { get; set; }
        }

        private class Candidate
        {
            [JsonPropertyName("content")]
            public Content? Content { get; set; }
        }

        private class Content
        {
            [JsonPropertyName("parts")]
            public List<Part>? Parts { get; set; }
        }

        private class Part
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        private class AiResponseFormat
        {
            public List<CourseMatch>? RecommendedCourses { get; set; }
            public string? ReasoningExplanation { get; set; }
            public string? LearningPath { get; set; }
            public List<string>? StudyTips { get; set; }
        }

        private class CourseMatch
        {
            public string CourseId { get; set; } = "";
            public decimal MatchScore { get; set; }
            public string MatchReason { get; set; } = "";
        }

        #endregion
    }
}
