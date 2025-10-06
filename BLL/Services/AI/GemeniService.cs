using BLL.IServices.AI;
using BLL.Settings;
using Common.DTO.Assement;
using Common.DTO.Learner;
using Common.DTO.Teacher;
using Microsoft.AspNetCore.Http;
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

              
                var cleanedResponse = response.Trim();

               
                if (cleanedResponse.StartsWith("```json"))
                {
                    cleanedResponse = cleanedResponse.Replace("```json", "").Replace("```", "").Trim();
                }
                else if (cleanedResponse.StartsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Replace("```", "").Trim();
                }

              
                cleanedResponse = cleanedResponse.Replace("**", "").Replace("*", "");

                
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
                                    MatchScore = Math.Min(100, Math.Max(0, rec.MatchScore)),
                                    MatchReason = rec.MatchReason ?? "Phù hợp với mục tiêu học tập",
                                    EstimatedDuration = course.Duration,
                                    Skills = course.Skills ?? new List<string>()
                                });

                                _logger.LogDebug("Matched course: {CourseId} - {Title}", course.CourseID, course.Title);
                            }
                            else
                            {
                                _logger.LogWarning("Course not found for ID: {CourseId}", rec.CourseId);

                               
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

      
        private List<string> GetDefaultStudyTips()
        {
            return new List<string>
    {
        "Học đều đặn mỗi ngày, dù chỉ 15-30 phút",
        "Luyện tập các kỹ năng nghe, nói, đọc, viết một cách cân bằng",
       
    };
        }

     
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

            
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("• ") || trimmed.StartsWith("* "))
                {
                    tips.Add(trimmed.Substring(2).Trim());
                }
             
                else if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+\.\s"))
                {
                    var dotIndex = trimmed.IndexOf('.');
                    if (dotIndex > 0 && dotIndex < trimmed.Length - 1)
                    {
                        tips.Add(trimmed.Substring(dotIndex + 1).Trim());
                    }
                }
               
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
        public async Task<TeacherQualificationAnalysisDto> AnalyzeTeacherQualificationsAsync(
    TeacherApplicationDto application,
    List<TeacherCredentialDto> credentials)
        {
            try
            {
                var prompt = BuildTeacherQualificationPrompt(application, credentials);
                var response = await CallGeminiApiAsync(prompt);

                return ParseTeacherQualificationResponse(response, application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing teacher qualifications for application {ApplicationId}", application.TeacherApplicationID);

                // Return fallback analysis
                return CreateFallbackQualificationAnalysis(application, credentials);
            }
        }

        private string BuildTeacherQualificationPrompt(TeacherApplicationDto application, List<TeacherCredentialDto> credentials)
        {
            var credentialsInfo = credentials.Select(c => new
            {
                name = c.CredentialName,
                type = c.Type.ToString(),
                url = c.CredentialFileUrl
            }).ToList();

            var credentialsJson = JsonSerializer.Serialize(credentialsInfo, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            return $@"# Phân tích trình độ giảng dạy của ứng viên giáo viên

## Thông tin ứng viên:
- **Tên ứng viên**: {application.UserName}
- **Ngôn ngữ ứng tuyển**: {application.LanguageName}
- **Động cơ ứng tuyển**: {application.Motivation}
- **Kinh nghiệm giảng dạy**: {application.TeachingExperience}
- **Chuyên môn**: {application.Specialization}
- **Cấp độ giảng dạy mong muốn**: {application.TeachingLevel}

## Danh sách bằng cấp và chứng chỉ:
{credentialsJson}

## Tiêu chí đánh giá theo cấp độ:

### 🟢 BEGINNER Level (Người mới bắt đầu):
- **Yêu cầu tối thiểu**: 
  - Bằng cấp liên quan đến ngôn ngữ (Cử nhân, Cao đẳng)
  - Chứng chỉ ngôn ngữ cơ bản (IELTS 6.5+, HSK 4+, JLPT N3+)
  - Có kinh nghiệm dạy học hoặc gia sư
- **Thích hợp cho**: Dạy phát âm cơ bản, từ vựng, ngữ pháp đơn giản

### 🟡 INTERMEDIATE Level (Trung cấp):
- **Yêu cầu**: 
  - Bằng Cử nhân chuyên ngành ngôn ngữ hoặc giáo dục
  - Chứng chỉ ngôn ngữ tốt (IELTS 7.5+, HSK 5+, JLPT N2+)
  - Chứng chỉ giảng dạy (TESOL, CELTA, hoặc tương đương)
  - Kinh nghiệm giảng dạy 1-3 năm
- **Thích hợp cho**: Dạy giao tiếp, ngữ pháp nâng cao, kỹ năng thực hành

### 🔴 ADVANCED Level (Nâng cao):
- **Yêu cầu**: 
  - Thạc sĩ trở lên về ngôn ngữ/giáo dục/ngôn ngữ học
  - Chứng chỉ ngôn ngữ xuất sắc (IELTS 8.0+, HSK 6, JLPT N1)
  - Chứng chỉ giảng dạy chuyên nghiệp cao cấp
  - Kinh nghiệm giảng dạy 3+ năm
  - Có kinh nghiệm với các khóa học chuyên sâu
- **Thích hợp cho**: Dạy business, academic, test preparation, văn hóa sâu

## Yêu cầu phân tích:
Hãy phân tích bằng cấp và chứng chỉ của ứng viên, sau đó đề xuất cấp độ giảng dạy phù hợp.

Trả về kết quả CHÍNH XÁC theo định dạng JSON như sau:

{{
    ""suggestedTeachingLevels"": [""Beginner"", ""Intermediate""],
    ""confidenceScore"": 85,
    ""reasoningExplanation"": ""Giải thích chi tiết về việc đánh giá và đề xuất cấp độ"",
    ""qualificationAssessments"": [
        {{
            ""credentialName"": ""Tên bằng cấp"",
            ""credentialType"": ""Degree/Certificate"",
            ""relevanceScore"": 90,
            ""assessment"": ""Đánh giá cụ thể về bằng cấp này"",
            ""supportedLevels"": [""Beginner"", ""Intermediate""]
        }}
    ],
    ""overallRecommendation"": ""Gợi ý tổng quan về việc phê duyệt và cấp độ được phép dạy"",
    ""teachingLevelSuggestions"": [
        {{
            ""level"": ""Beginner"",
            ""confidenceScore"": 95,
            ""justification"": ""Lý do tại sao phù hợp với cấp độ này"",
            ""isRecommended"": true
        }},
        {{
            ""level"": ""Intermediate"",
            ""confidenceScore"": 70,
            ""justification"": ""Có thể dạy được nhưng cần thêm kinh nghiệm"",
            ""isRecommended"": false
        }}
    ]
}}

**Lưu ý quan trọng**: 
- Hãy đánh giá khách quan và chặt chẽ
- Chỉ đề xuất cấp độ mà ứng viên thực sự có đủ trình độ
- Ưu tiên chất lượng giảng dạy hơn là số lượng cấp độ
- Xem xét cả kinh nghiệm thực tế và bằng cấp lý thuyết

Trả lời bằng tiếng Việt, chi tiết và có căn cứ rõ ràng.";
        }
        private TeacherQualificationAnalysisDto ParseTeacherQualificationResponse(string response, TeacherApplicationDto application)
        {
            try
            {
                _logger.LogDebug("Parsing teacher qualification analysis: {Response}", response);

                // Clean the response
                var cleanedResponse = response.Trim();
                if (cleanedResponse.StartsWith("```json"))
                {
                    cleanedResponse = cleanedResponse.Replace("```json", "").Replace("```", "").Trim();
                }
                else if (cleanedResponse.StartsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Replace("```", "").Trim();
                }

                // Find JSON boundaries
                var jsonStart = cleanedResponse.IndexOf('{');
                var jsonEnd = cleanedResponse.LastIndexOf('}') + 1;

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonResponse = cleanedResponse.Substring(jsonStart, jsonEnd - jsonStart);

                    var aiResponse = JsonSerializer.Deserialize<TeacherQualificationAiResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    });

                    if (aiResponse != null)
                    {
                        return new TeacherQualificationAnalysisDto
                        {
                            ApplicationId = application.TeacherApplicationID,
                            LanguageName = application.LanguageName,
                            SuggestedTeachingLevels = aiResponse.SuggestedTeachingLevels ?? new List<string>(),
                            ConfidenceScore = Math.Min(100, Math.Max(0, aiResponse.ConfidenceScore)),
                            ReasoningExplanation = aiResponse.ReasoningExplanation ?? "",
                            QualificationAssessments = aiResponse.QualificationAssessments?.Select(qa => new QualificationAssessment
                            {
                                CredentialName = qa.CredentialName ?? "",
                                CredentialType = qa.CredentialType ?? "",
                                RelevanceScore = Math.Min(100, Math.Max(0, qa.RelevanceScore)),
                                Assessment = qa.Assessment ?? "",
                                SupportedLevels = qa.SupportedLevels ?? new List<string>()
                            }).ToList() ?? new List<QualificationAssessment>(),
                            OverallRecommendation = aiResponse.OverallRecommendation ?? "",
                            AnalyzedAt = DateTime.UtcNow
                        };
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error in teacher qualification analysis");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing teacher qualification analysis");
            }

            // Return fallback if parsing fails
            return CreateFallbackQualificationAnalysis(application, new List<TeacherCredentialDto>());
        }

        private TeacherQualificationAnalysisDto CreateFallbackQualificationAnalysis(TeacherApplicationDto application, List<TeacherCredentialDto> credentials)
        {
            // Basic analysis based on available info
            var suggestedLevels = new List<string> { "Beginner" }; // Conservative default

            // If they have teaching experience, might allow Intermediate
            if (!string.IsNullOrEmpty(application.TeachingExperience) &&
                application.TeachingExperience.Length > 50)
            {
                suggestedLevels.Add("Intermediate");
            }

            return new TeacherQualificationAnalysisDto
            {
                ApplicationId = application.TeacherApplicationID,
                LanguageName = application.LanguageName,
                SuggestedTeachingLevels = suggestedLevels,
                ConfidenceScore = 50, // Low confidence for fallback
                ReasoningExplanation = "Không thể phân tích chi tiết bằng AI. Đánh giá dựa trên thông tin cơ bản có sẵn. Vui lòng xem xét thủ công các bằng cấp được nộp.",
                QualificationAssessments = credentials.Select(c => new QualificationAssessment
                {
                    CredentialName = c.CredentialName,
                    CredentialType = c.Type.ToString(),
                    RelevanceScore = 70,
                    Assessment = "Cần xem xét thủ công",
                    SupportedLevels = new List<string> { "Beginner" }
                }).ToList(),
                OverallRecommendation = "Gợi ý cho phép dạy cấp độ Beginner. Cần đánh giá thủ công để xác định các cấp độ khác.",
                AnalyzedAt = DateTime.UtcNow
            };
        }
        public async Task<VoiceEvaluationResult> EvaluateVoiceResponseDirectlyAsync(VoiceAssessmentQuestion question, IFormFile audioFile, string languageCode)
        {
            try
            {
          
                var audioBase64 = await ConvertAudioToBase64Async(audioFile);
                var prompt = BuildVoiceEvaluationPromptWithAudio(question, languageCode);

                
                var response = await CallGeminiApiWithAudioAsync(prompt, audioBase64, audioFile.ContentType);
                return ParseVoiceEvaluationResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating voice response directly for question {QuestionNumber}", question.QuestionNumber);
                return CreateFallbackVoiceEvaluation();
            }
        }

      
        private async Task<string> ConvertAudioToBase64Async(IFormFile audioFile)
        {
            try
            {
                // Validate audio file
                var allowedTypes = new[] { "audio/mp3", "audio/wav", "audio/m4a", "audio/webm", "audio/mpeg" };
                if (!allowedTypes.Contains(audioFile.ContentType.ToLower()))
                    throw new ArgumentException("Chỉ hỗ trợ file âm thanh MP3, WAV, M4A, WebM");

                // Max file size: 10MB
                if (audioFile.Length > 10 * 1024 * 1024)
                    throw new ArgumentException("File âm thanh không được vượt quá 10MB");

                using var memoryStream = new MemoryStream();
                await audioFile.CopyToAsync(memoryStream);
                var audioBytes = memoryStream.ToArray();
                return Convert.ToBase64String(audioBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting audio to base64");
                throw;
            }
        }

   
        private string BuildVoiceEvaluationPromptWithAudio(VoiceAssessmentQuestion question, string languageCode)
        {
            var languageStandards = GetLanguageStandards(languageCode);
            var languageName = GetLanguageName(languageCode);

            return $@"# 🌍 ĐÁNH GIÁ GIỌNG NÓI {languageName.ToUpper()} TỪ FILE ÂM THANH

## ⚠️ QUAN TRỌNG: LANGUAGE VALIDATION
**NGÔN NGỮ ĐÁNH GIÁ**: {languageName} ({languageCode})
**FRAMEWORK**: {GetStandardName(languageCode)}

🚨 **CRITICAL**: Audio PHẢI là {languageName}. Nếu phát hiện ngôn ngữ khác, báo lỗi ngay!

## Thông tin câu hỏi:
**Cấp độ**: {question.Difficulty}
**Loại**: {question.QuestionType}
**Câu hỏi**: {question.Question}
**Yêu cầu**: {question.PromptText}

## Tiêu chuẩn đánh giá {languageName}:
{languageStandards}

## 🔍 BƯỚC 1: LANGUAGE DETECTION
Trước khi đánh giá, hãy XÁC ĐỊNH NGÔN NGỮ trong audio:
- Nếu audio là {languageName} → Tiếp tục đánh giá
- Nếu audio KHÔNG phải {languageName} → Trả về lỗi ngay lập tức

## 🎯 BƯỚC 2: ĐÁNH GIÁ (chỉ khi audio đúng ngôn ngữ)

### 1. Phát âm (Pronunciation) - 30%
- Độ chính xác phát âm từng từ
- Ngữ điệu và trọng âm đúng
- Âm thanh rõ ràng, dễ hiểu
- Phát âm các âm vị khó

### 2. Độ lưu loát (Fluency) - 25%
- Tốc độ nói phù hợp với cấp độ
- Ít ngập ngừng, lặp từ
- Kết nối tự nhiên giữa các từ/câu
- Nhịp điệu tự nhiên

### 3. Ngữ pháp (Grammar) - 25%
- Cấu trúc câu đúng ngữ pháp
- Sử dụng thì và dạng từ chính xác
- Trật tự từ phù hợp với ngôn ngữ
- Độ phức tạp phù hợp với cấp độ

### 4. Từ vựng (Vocabulary) - 20%
- Phạm vi từ vựng phong phú
- Sử dụng từ chính xác trong ngữ cảnh
- Đa dạng trong cách diễn đạt
- Từ vựng phù hợp với chủ đề

## Yêu cầu đặc biệt cho {languageName}:
{GetLanguageSpecificCriteria(languageCode)}

## Format trả về:

### Nếu ĐÚNG ngôn ngữ {languageName}:
{{
    ""languageDetected"": ""{languageCode}"",
    ""isCorrectLanguage"": true,
    ""overallScore"": 85,
    ""pronunciation"": {{
        ""score"": 80,
        ""level"": ""Good"",
        ""mispronuncedWords"": [""từ phát âm sai""],
        ""feedback"": ""Phân tích chi tiết về phát âm""
    }},
    ""fluency"": {{
        ""score"": 90,
        ""speakingRate"": 150,
        ""pauseCount"": 3,
        ""rhythm"": ""Natural"",
        ""feedback"": ""Đánh giá về độ lưu loát""
    }},
    ""grammar"": {{
        ""score"": 85,
        ""grammarErrors"": [""lỗi ngữ pháp cụ thể""],
        ""structureAssessment"": ""Đánh giá cấu trúc câu"",
        ""feedback"": ""Phân tích ngữ pháp chi tiết""
    }},
    ""vocabulary"": {{
        ""score"": 80,
        ""rangeAssessment"": ""Good"",
        ""accuracyAssessment"": ""Mostly accurate"",
        ""feedback"": ""Đánh giá từ vựng""
    }},
    ""detailedFeedback"": ""Đánh giá tổng quan chi tiết về khả năng nói..."",
    ""strengths"": [""Điểm mạnh 1"", ""Điểm mạnh 2""],
    ""areasForImprovement"": [""Cần cải thiện 1"", ""Cần cải thiện 2""]
}}

### Nếu SAI ngôn ngữ:
{{
    ""languageDetected"": ""detected_language_code"",
    ""isCorrectLanguage"": false,
    ""error"": ""LANGUAGE_MISMATCH"",
    ""message"": ""Audio được phát hiện là [detected_language] nhưng assessment yêu cầu {languageName}"",
    ""expectedLanguage"": ""{languageCode}"",
    ""detectedLanguage"": ""detected_language_code"",
    ""overallScore"": 0,
    ""pronunciation"": {{
        ""score"": 0,
        ""level"": ""Language Error"",
        ""feedback"": ""❌ Sai ngôn ngữ: Expected {languageName}, detected [detected_language]""
    }},
    ""detailedFeedback"": ""🚨 **Lỗi ngôn ngữ**: Bạn đã gửi audio [detected_language] nhưng bài test này yêu cầu {languageName}. Vui lòng ghi âm lại bằng {languageName}.""
}}

**🚨 LƯU Ý CRITICAL**: 
- LUÔN LUÔN kiểm tra ngôn ngữ trước khi đánh giá
- Nếu sai ngôn ngữ, PHẢI trả về error format
- Không bao giờ đánh giá tiếng Anh theo JLPT hay tiếng Nhật theo CEFR!";
        }

     
        private string GetLanguageSpecificCriteria(string languageCode)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => @"
- **Stress patterns**: Đánh giá trọng âm từ và câu
- **Intonation**: Ngữ điệu lên xuống tự nhiên
- **Connected speech**: Liên kết âm giữa các từ
- **Vowel sounds**: Đặc biệt chú ý các nguyên âm khó",

                "ZH" => @"
- **Tones**: Đánh giá 4 thanh điệu chính xác
- **Initials & Finals**: Âm đầu và âm cuối chuẩn
- **Tone changes**: Biến điệu thanh trong từ ghép
- **Rhythm**: Nhịp điệu đặc trưng tiếng Trung",

                "JP" => @"
- **Pitch accent**: Trọng âm cao thấp đúng
- **Mora timing**: Nhịp điệu đều đặn
- **Long vowels**: Nguyên âm dài chính xác
- **Consonant clusters**: Cụm phụ âm đúng",

                _ => "Đánh giá theo tiêu chuẩn chung của ngôn ngữ"
            };
        }


        private async Task<string> CallGeminiApiWithAudioAsync(string prompt, string audioBase64, string mimeType)
        {
            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = audioBase64
                            }
                        }
                    }
                }
            },
                    generationConfig = new
                    {
                        temperature = 0.4,
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

             
                var model = "gemini-2.5-flash-lite"; 
                var url = $"{_settings.BaseUrl}/models/{model}:generateContent?key={_settings.ApiKey}";

                _logger.LogInformation("Calling Gemini API with audio: {Url}", url);
                _logger.LogInformation("Request size: {Size} bytes", jsonContent.Length);

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Response status: {Status}, Content length: {Length}",
                    response.StatusCode, responseContent.Length);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    throw new HttpRequestException($"Gemini API returned {response.StatusCode}: {responseContent}");
                }

           
                if (string.IsNullOrEmpty(responseContent))
                {
                    _logger.LogError("Gemini API returned empty response");
                    throw new HttpRequestException("Gemini API returned empty response");
                }

                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var result = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";

             
                if (string.IsNullOrEmpty(result))
                {
                    _logger.LogError("Gemini API returned valid JSON but empty text content");
                    throw new HttpRequestException("Gemini API returned empty text content");
                }

                _logger.LogInformation("Gemini API audio response received: {Length} characters", result.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API with audio");
                throw;
            }
        }


//        public async Task<VoiceAssessmentResultDto> GenerateVoiceAssessmentResultAsync(
//    string languageCode,
//    string languageName,
//    List<VoiceAssessmentQuestion> questions,
//    string? goalName = null)
//        {
//            try
//            {
//                var prompt = BuildVoiceAssessmentResultPrompt(languageCode, languageName, questions, goalName);
//                var response = await CallGeminiApiAsync(prompt);
//                return ParseVoiceAssessmentResult(response, languageName, questions);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating voice assessment result");
//                return CreateFallbackVoiceAssessmentResult(languageName, questions);
//            }
//        }

//        private string BuildVoiceAssessmentResultPrompt(
//            string languageCode,
//            string languageName,
//            List<VoiceAssessmentQuestion> questions,
//            string? goalName = null)
//        {
//            var completedQuestions = questions.Where(q => !q.IsSkipped && q.EvaluationResult != null).ToList();
//            var completedCount = completedQuestions.Count;
//            var totalQuestions = questions.Count;

//            var questionsJson = JsonSerializer.Serialize(completedQuestions.Select(q => new {
//                q.QuestionNumber,
//                q.Difficulty,
//                OverallScore = q.EvaluationResult?.OverallScore ?? 0,
//                PronunciationScore = q.EvaluationResult?.Pronunciation?.Score ?? 0,
//                FluencyScore = q.EvaluationResult?.Fluency?.Score ?? 0,
//                GrammarScore = q.EvaluationResult?.Grammar?.Score ?? 0,
//                VocabularyScore = q.EvaluationResult?.Vocabulary?.Score ?? 0
//            }), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

//            var goalContext = !string.IsNullOrEmpty(goalName)
//                ? $"\n\n## Mục tiêu học tập:\n**Goal**: {goalName}\n\n*Lưu ý: Roadmap và gợi ý cần phù hợp với mục tiêu '{goalName}'*"
//                : "";

//            var standardFramework = GetStandardFramework(languageCode);

//            return $@"# Phân tích kết quả đánh giá giọng nói {languageName}

//## Thông tin bài test:
//- **Số câu hoàn thành**: {completedCount}/{totalQuestions}
//- **Độ tin cậy đánh giá**: {GetConfidenceLevel(completedCount, totalQuestions)}%
//{goalContext}

//## Dữ liệu điểm chi tiết:
//{questionsJson}

//## Khung chuẩn {GetStandardName(languageCode)}:
//{standardFramework}

//## YÊU CẦU ĐÁNH GIÁ:

//### 1. Đánh giá CHÍNH XÁC dựa trên {completedCount} câu đã làm:
//- KHÔNG tự động cho điểm 70 hay bất kỳ điểm mặc định nào
//- Tính điểm dựa 100% trên câu đã hoàn thành
//- Nêu rõ giới hạn nếu completedCount < {totalQuestions}

//### 2. Xác định Level theo khung chuẩn {GetStandardName(languageCode)}:
//{GetLevelDeterminationRules(languageCode)}

//### 3. Cung cấp Roadmap phù hợp với Goal:
//{(!string.IsNullOrEmpty(goalName) ? $"- Roadmap phải hướng tới mục tiêu '{goalName}'" : "")}
//- Các phase phải cụ thể và thực tế
//- Thời gian ước tính hợp lý

//## Format JSON trả về:
//{{
//    ""determinedLevel"": ""{GetExampleLevel(languageCode)}"",
//    ""levelConfidence"": {GetConfidenceLevel(completedCount, totalQuestions)},
//    ""assessmentCompleteness"": ""{completedCount}/{totalQuestions} câu"",
//    ""overallScore"": <điểm trung bình từ {completedCount} câu>,
//    ""pronunciationScore"": <điểm trung bình pronunciation>,
//    ""fluencyScore"": <điểm trung bình fluency>,
//    ""grammarScore"": <điểm trung bình grammar>,
//    ""vocabularyScore"": <điểm trung bình vocabulary>,
//    ""detailedFeedback"": ""Dựa trên {completedCount}/{totalQuestions} câu đã hoàn thành...\n\n{(completedCount < totalQuestions ? $"⚠️ **Lưu ý**: Kết quả này có độ tin cậy {GetConfidenceLevel(completedCount, totalQuestions)}%. Để có đánh giá chính xác hơn, vui lòng hoàn thành đủ {totalQuestions} câu." : "")}"",
//    ""keyStrengths"": [""Điểm mạnh từ {completedCount} câu""],
//    ""improvementAreas"": [""Cần cải thiện""{(completedCount < totalQuestions ? $", \"Hoàn thành thêm {totalQuestions - completedCount} câu\"" : "")}],
//    ""nextLevelRequirements"": ""Để đạt level [{GetNextLevel(languageCode)}], cần..."",
//    ""roadmap"": {{
//        ""currentLevel"": ""{GetExampleLevel(languageCode)}"",
//        ""targetLevel"": ""{GetNextLevel(languageCode)}"",
//        ""phases"": [
//            {{
//                ""phaseNumber"": 1,
//                ""title"": ""Phase phù hợp với mục tiêu {goalName ?? "học tập"}"",
//                ""duration"": ""4-8 tuần"",
//                ""goals"": [""Mục tiêu cụ thể""],
//                ""practiceActivities"": [""Hoạt động luyện tập""]
//            }}
//        ]
//    }}
//}}

//**LƯU Ý QUAN TRỌNG**:
//- Điểm số = average của {completedCount} câu thực tế (KHÔNG phải điểm giả định)
//- Level = xác định theo khung {GetStandardName(languageCode)} chính thức
//- Confidence = {GetConfidenceLevel(completedCount, totalQuestions)}% (giảm nếu thiếu câu)";
//        }

        private int GetConfidenceLevel(int completed, int total)
        {
            return completed switch
            {
                0 => 0,
                1 => 40,
                2 => 60,
                3 => 80,
                _ when completed >= total => 95,
                _ => 50
            };
        }
//        private string BuildVoiceAssessmentResultPrompt(string languageCode, string languageName, List<VoiceAssessmentQuestion> questions)
//        {
//            var completedQuestions = questions.Where(q => !q.IsSkipped && q.EvaluationResult != null).ToList();
//            var totalQuestions = questions.Count;
//            var completedCount = completedQuestions.Count;

//            var questionsJson = JsonSerializer.Serialize(completedQuestions.Select(q => new {
//                q.QuestionNumber,
//                q.Question,
//                q.Difficulty,
//                q.QuestionType,
//                OverallScore = q.EvaluationResult?.OverallScore ?? 0,
//                PronunciationScore = q.EvaluationResult?.Pronunciation?.Score ?? 0,
//                FluencyScore = q.EvaluationResult?.Fluency?.Score ?? 0,
//                GrammarScore = q.EvaluationResult?.Grammar?.Score ?? 0,
//                VocabularyScore = q.EvaluationResult?.Vocabulary?.Score ?? 0
//            }), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

//            var standardFramework = GetStandardFramework(languageCode);

//            return $@"# Phân tích kết quả đánh giá giọng nói {languageName}

//## Thông tin bài test:
//- **Tổng số câu**: {totalQuestions}
//- **Số câu đã hoàn thành**: {completedCount}
//- **Số câu bỏ qua**: {totalQuestions - completedCount}

//## Dữ liệu câu hỏi và điểm chi tiết:
//{questionsJson}

//## Khung chuẩn đánh giá {languageName}:
//{standardFramework}

//## YÊU CẦU QUAN TRỌNG:

//### 1. Đánh giá dựa trên số câu thực tế:
//- Nếu làm 1/4 câu: Đánh giá dựa trên 1 câu đó, không extrapolate
//- Nếu làm 2/4 câu: Đánh giá dựa trên 2 câu đó
//- Nếu làm 3/4 câu: Đánh giá dựa trên 3 câu đó
//- Nếu làm đủ 4/4 câu: Đánh giá toàn diện

//### 2. Xác định cấp độ CHÍNH XÁC theo khung chuẩn:
//{GetLevelDeterminationRules(languageCode)}

//### 3. Điểm số phải phản ánh chính xác khả năng:
//- Không tự động cho điểm trung bình nếu thiếu dữ liệu
//- Nêu rõ giới hạn đánh giá do số câu ít
//- Đề xuất làm thêm câu nếu cần đánh giá chính xác hơn

//## Format trả về (JSON):
//{{
//    ""determinedLevel"": ""{GetExampleLevel(languageCode)}"",
//    ""levelConfidence"": 85,
//    ""assessmentCompleteness"": ""{completedCount}/{totalQuestions} câu"",
//    ""overallScore"": 75,
//    ""pronunciationScore"": 80,
//    ""fluencyScore"": 70,
//    ""grammarScore"": 75,
//    ""vocabularyScore"": 75,
//    ""detailedFeedback"": ""Dựa trên {completedCount} câu đã hoàn thành, khả năng speaking của bạn...\n\n⚠️ Lưu ý: Đánh giá này dựa trên {completedCount}/{totalQuestions} câu. Để có kết quả chính xác hơn, bạn nên hoàn thành tất cả câu hỏi."",
//    ""keyStrengths"": [""Điểm mạnh cụ thể từ {completedCount} câu""],
//    ""improvementAreas"": [""Cần cải thiện cụ thể""],
//    ""nextLevelRequirements"": ""Để đạt cấp độ tiếp theo [{GetNextLevel(languageCode)}], bạn cần..."",
//    ""roadmap"": {{
//        ""currentLevel"": ""{GetExampleLevel(languageCode)}"",
//        ""targetLevel"": ""{GetNextLevel(languageCode)}"",
//        ""estimatedTimeToNextLevel"": ""3-6 tháng với luyện tập đều đặn"",
//        ""phases"": [...]
//    }}
//}}

//**LƯU Ý**: 
//- Đánh giá phải dựa 100% trên {completedCount} câu đã hoàn thành
//- Trả về cấp độ CHÍNH XÁC theo khung {GetStandardName(languageCode)}
//- Không đoán mò hay extrapolate nếu thiếu dữ liệu
//- Nêu rõ giới hạn của đánh giá nếu số câu < 4";
//        }

     

        private string GetStandardFramework(string languageCode)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => @"
### CEFR Framework (Common European Framework of Reference)

**A1 (Beginner):**
- Pronunciation: Basic sounds, heavy accent acceptable
- Fluency: Slow, frequent pauses (60-80 words/min)
- Grammar: Simple present, basic sentences
- Vocabulary: 500-800 words
- Can handle: Greetings, basic personal info

**A2 (Elementary):**
- Pronunciation: Clearer, some mistakes okay
- Fluency: Still slow but smoother (80-100 words/min)
- Grammar: Present/past tenses, simple connectors
- Vocabulary: 1000-1500 words
- Can handle: Daily routines, simple descriptions

**B1 (Intermediate):**
- Pronunciation: Generally clear, minor accent
- Fluency: Natural pace (100-120 words/min)
- Grammar: Most tenses, complex sentences
- Vocabulary: 2000-3000 words
- Can handle: Opinions, experiences, explanations

**B2 (Upper-Intermediate):**
- Pronunciation: Clear, natural intonation
- Fluency: Smooth, confident (120-140 words/min)
- Grammar: Advanced structures, conditionals
- Vocabulary: 3500-5000 words
- Can handle: Abstract topics, arguments

**C1 (Advanced):**
- Pronunciation: Near-native, subtle errors
- Fluency: Effortless (140-160 words/min)
- Grammar: Complex, sophisticated structures
- Vocabulary: 6000-8000 words
- Can handle: Complex discussions, nuanced ideas

**C2 (Proficient):**
- Pronunciation: Native-like
- Fluency: Natural, idiomatic (160+ words/min)
- Grammar: Flawless, stylistic variety
- Vocabulary: 10000+ words
- Can handle: Any topic with precision",

                "ZH" => @"
### HSK Framework (Hanyu Shuiping Kaoshi)

**HSK 1 (Beginner):**
- Tones: Can produce 4 tones but inconsistent
- Pronunciation: Basic initials/finals, many errors
- Fluency: Very slow, word-by-word
- Vocabulary: 150-300 characters
- Can handle: Self-introduction, very basic phrases

**HSK 2 (Elementary):**
- Tones: More consistent, occasional errors
- Pronunciation: Clearer but still learning
- Fluency: Slow, short sentences
- Vocabulary: 300-600 characters
- Can handle: Simple daily conversations

**HSK 3 (Pre-Intermediate):**
- Tones: Generally accurate (80%+)
- Pronunciation: Clear enough to understand
- Fluency: Can speak in paragraphs
- Vocabulary: 600-1200 characters
- Can handle: Daily life, work, study topics

**HSK 4 (Intermediate):**
- Tones: Accurate (90%+), natural tone changes
- Pronunciation: Clear, proper rhythm
- Fluency: Smooth, few hesitations
- Vocabulary: 1200-2500 characters
- Can handle: Complex topics, discussions

**HSK 5 (Upper-Intermediate):**
- Tones: Highly accurate, natural flow
- Pronunciation: Clear, proper stress patterns
- Fluency: Natural pace, good coherence
- Vocabulary: 2500-5000 characters
- Can handle: Abstract topics, Chinese media

**HSK 6 (Advanced):**
- Tones: Native-like accuracy and variation
- Pronunciation: Excellent, subtle nuances
- Fluency: Effortless, idiomatic expressions
- Vocabulary: 5000+ characters, chengyu usage
- Can handle: Professional, academic discussions",

                "JP" => @"
### JLPT Framework (Japanese Language Proficiency Test)

**N5 (Beginner):**
- Pitch Accent: Learning basic patterns
- Pronunciation: Can produce hiragana sounds
- Fluency: Very slow, word-by-word
- Grammar: Basic particles, verb forms
- Vocabulary: 800 words, 100 kanji
- Can handle: Self-introduction, basic needs

**N4 (Elementary):**
- Pitch Accent: Improving, still inconsistent
- Pronunciation: Clearer, proper long vowels
- Fluency: Short sentences, hesitations
- Grammar: Basic verb conjugations, て-form
- Vocabulary: 1500 words, 300 kanji
- Can handle: Daily conversations, simple requests

**N3 (Intermediate):**
- Pitch Accent: More natural, most words correct
- Pronunciation: Clear, proper mora timing
- Fluency: Can maintain conversation
- Grammar: -tai, -たら, potential form
- Vocabulary: 3750 words, 650 kanji
- Can handle: Work situations, explanations

**N2 (Upper-Intermediate):**
- Pitch Accent: Natural, compound words correct
- Pronunciation: Clear, natural rhythm
- Fluency: Smooth, appropriate pauses
- Grammar: Keigo basics, complex sentences
- Vocabulary: 6000 words, 1000 kanji
- Can handle: Abstract topics, business Japanese

**N1 (Advanced):**
- Pitch Accent: Native-like precision
- Pronunciation: Excellent, natural assimilation
- Fluency: Effortless, idiomatic usage
- Grammar: Advanced keigo, literary forms
- Vocabulary: 10000 words, 2000+ kanji
- Can handle: Professional, academic contexts",

                _ => "Standard language proficiency framework"
            };
        }

        private string GetLevelDeterminationRules(string languageCode)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => @"
**Quy tắc xác định CEFR Level:**
- A1: Overall 20-40, can produce basic sounds
- A2: Overall 40-55, simple sentences with errors
- B1: Overall 55-70, understandable with some effort
- B2: Overall 70-85, clear and relatively fluent
- C1: Overall 85-95, sophisticated and natural
- C2: Overall 95-100, near-native proficiency

**Thang đo chi tiết:**
- Pronunciation score trọng số 30%
- Fluency score trọng số 25%
- Grammar score trọng số 25%
- Vocabulary score trọng số 20%",

                "ZH" => @"
**Quy tắc xác định HSK Level:**
- HSK 1: Overall 20-35, basic tone production
- HSK 2: Overall 35-50, simple phrases clear
- HSK 3: Overall 50-65, understandable Chinese
- HSK 4: Overall 65-80, good fluency and accuracy
- HSK 5: Overall 80-90, near-native fluency
- HSK 6: Overall 90-100, native-like proficiency

**Đặc biệt chú trọng Tones (40% trọng số):**
- Tones accurate < 70%: Max HSK 2
- Tones accurate 70-85%: HSK 3-4
- Tones accurate > 85%: HSK 5-6",

                "JP" => @"
**Quy tắc xác định JLPT Level:**
- N5: Overall 20-35, basic hiragana pronunciation
- N4: Overall 35-50, simple Japanese understandable
- N3: Overall 50-70, daily conversation capable
- N2: Overall 70-85, business Japanese capable
- N1: Overall 85-100, native-like proficiency

**Đặc biệt chú trọng Pitch Accent (35% trọng số):**
- Pitch errors > 50%: Max N4
- Pitch errors 30-50%: N3-N2
- Pitch errors < 30%: N2-N1",

                _ => "Standard proficiency determination rules"
            };
        }

        private string GetExampleLevel(string languageCode)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => "B1",
                "ZH" => "HSK 3",
                "JP" => "N3",
                _ => "Intermediate"
            };
        }

        private string GetNextLevel(string languageCode)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => "B2",
                "ZH" => "HSK 4",
                "JP" => "N2",
                _ => "Advanced"
            };
        }

        private string GetStandardName(string languageCode)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => "CEFR",
                "ZH" => "HSK",
                "JP" => "JLPT",
                _ => "Standard"
            };
        }

        // Helper methods for parsing and fallback
        private VoiceAssessmentResultDto ParseVoiceAssessmentResult(string response, string languageName, List<VoiceAssessmentQuestion> questions)
        {
            try
            {
                var cleanedResponse = CleanJsonResponse(response);
                var result = JsonSerializer.Deserialize<VoiceAssessmentResultDto>(cleanedResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (result != null)
                {
                    result.LanguageName = languageName;
                    result.CompletedAt = DateTime.UtcNow;
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing voice assessment result");
            }

            return CreateFallbackVoiceAssessmentResult(languageName, questions);
        }
        private VoiceAssessmentResultDto CreateFallbackVoiceAssessmentResult(string languageName, List<VoiceAssessmentQuestion> questions)
        {
            var totalQuestions = questions.Count;

            return new VoiceAssessmentResultDto
            {
                LanguageName = languageName,
                DeterminedLevel = "Unassessed",
                LevelConfidence = 0,
                AssessmentCompleteness = $"0/{totalQuestions} câu",
                OverallScore = 0,
                PronunciationScore = 0,
                FluencyScore = 0,
                GrammarScore = 0,
                VocabularyScore = 0,
                DetailedFeedback = $"⚠️ Không thể đánh giá do lỗi xử lý AI.\n\nVui lòng thử lại hoặc liên hệ support.",
                KeyStrengths = new List<string> { "Đã tham gia bài test" },
                ImprovementAreas = new List<string>
        {
            "Cần hoàn thành bài đánh giá lại",
            "Liên hệ support nếu vấn đề tiếp diễn"
        },
                NextLevelRequirements = "Hoàn thành bài test để biết yêu cầu cấp độ tiếp theo",
                RecommendedCourses = new List<RecommendedCourseDto>(),
                CompletedAt = DateTime.UtcNow
            };
        }
        //private VoiceAssessmentResultDto CreateFallbackVoiceAssessmentResult(string languageName, List<VoiceAssessmentQuestion> questions)
        //{
        //    var completedQuestions = questions.Where(q => !q.IsSkipped && q.EvaluationResult != null).ToList();
        //    var totalQuestions = questions.Count;
        //    var completedCount = completedQuestions.Count;

        //    // ⚠️ KHÔNG CHO ĐIỂM MẶC ĐỊNH - Đánh giá dựa trên câu thực tế
        //    if (completedCount == 0)
        //    {
        //        return new VoiceAssessmentResultDto
        //        {
        //            LanguageName = languageName,
        //            DeterminedLevel = "Unassessed",
        //            LevelConfidence = 0,
        //            AssessmentCompleteness = $"0/{totalQuestions} câu",
        //            OverallScore = 0,
        //            PronunciationScore = 0,
        //            FluencyScore = 0,
        //            GrammarScore = 0,
        //            VocabularyScore = 0,
        //            DetailedFeedback = $"⚠️ Không thể đánh giá vì chưa hoàn thành câu nào.\n\nVui lòng hoàn thành ít nhất 2-3 câu để có kết quả đánh giá chính xác.",
        //            KeyStrengths = new List<string> { "Đã tham gia bài test" },
        //            ImprovementAreas = new List<string> {
        //        "Cần hoàn thành các câu hỏi để được đánh giá",
        //        $"Còn {totalQuestions} câu chưa làm"
        //    },
        //            NextLevelRequirements = "Hoàn thành bài test để biết yêu cầu cấp độ tiếp theo",
        //            CompletedAt = DateTime.UtcNow
        //        };
        //    }

        //    // Tính điểm dựa trên câu đã hoàn thành
        //    var avgPronunciation = (int)completedQuestions.Average(q => q.EvaluationResult!.Pronunciation.Score);
        //    var avgFluency = (int)completedQuestions.Average(q => q.EvaluationResult!.Fluency.Score);
        //    var avgGrammar = (int)completedQuestions.Average(q => q.EvaluationResult!.Grammar.Score);
        //    var avgVocabulary = (int)completedQuestions.Average(q => q.EvaluationResult!.Vocabulary.Score);
        //    var avgOverall = (int)completedQuestions.Average(q => q.EvaluationResult!.OverallScore);

        //    // Giảm confidence nếu thiếu dữ liệu
        //    var confidence = completedCount switch
        //    {
        //        1 => 40, // 1/4 câu - confidence thấp
        //        2 => 60, // 2/4 câu - confidence trung bình
        //        3 => 80, // 3/4 câu - confidence khá
        //        4 => 95, // 4/4 câu - confidence cao
        //        _ => 50
        //    };

        //    var level = DetermineLevelFromScore(avgOverall, languageName);
        //    var completenessWarning = completedCount < totalQuestions
        //        ? $"\n\n⚠️ **Giới hạn đánh giá**: Kết quả này dựa trên {completedCount}/{totalQuestions} câu. Độ tin cậy: {confidence}%. Để có đánh giá chính xác hơn, vui lòng hoàn thành tất cả {totalQuestions} câu."
        //        : "";

        //    return new VoiceAssessmentResultDto
        //    {
        //        LanguageName = languageName,
        //        DeterminedLevel = level,
        //        LevelConfidence = confidence,
        //        AssessmentCompleteness = $"{completedCount}/{totalQuestions} câu",
        //        OverallScore = avgOverall,
        //        PronunciationScore = avgPronunciation,
        //        FluencyScore = avgFluency,
        //        GrammarScore = avgGrammar,
        //        VocabularyScore = avgVocabulary,
        //        DetailedFeedback = $"Dựa trên {completedCount} câu đã hoàn thành:\n\n" +
        //                          $"Khả năng speaking {languageName} của bạn được đánh giá ở cấp độ **{level}** với điểm tổng thể {avgOverall}/100." +
        //                          completenessWarning,
        //        KeyStrengths = ExtractStrengths(completedQuestions, completedCount),
        //        ImprovementAreas = ExtractImprovements(completedQuestions, completedCount, totalQuestions),
        //        NextLevelRequirements = GetNextLevelRequirement(level, languageName),
        //        Roadmap = new VoiceLearningRoadmapDto
        //        {
        //            CurrentLevel = level,
        //            TargetLevel = GetNextLevelForLanguage(level, languageName),
        //            VocalPracticeTips = GetDefaultVocalTips(languageName)
        //        },
        //        CompletedAt = DateTime.UtcNow
        //    };
        //}

        // Helper method - Xác định level từ điểm số
        private string DetermineLevelFromScore(int overallScore, string languageName)
        {
            // Xác định dựa trên ngôn ngữ
            if (languageName.Contains("Anh") || languageName.Contains("English"))
            {
                return overallScore switch
                {
                    >= 95 => "C2",
                    >= 85 => "C1",
                    >= 70 => "B2",
                    >= 55 => "B1",
                    >= 40 => "A2",
                    >= 20 => "A1",
                    _ => "Below A1"
                };
            }
            else if (languageName.Contains("Trung") || languageName.Contains("Chinese"))
            {
                return overallScore switch
                {
                    >= 90 => "HSK 6",
                    >= 80 => "HSK 5",
                    >= 65 => "HSK 4",
                    >= 50 => "HSK 3",
                    >= 35 => "HSK 2",
                    >= 20 => "HSK 1",
                    _ => "Below HSK 1"
                };
            }
            else if (languageName.Contains("Nhật") || languageName.Contains("Japanese"))
            {
                return overallScore switch
                {
                    >= 85 => "N1",
                    >= 70 => "N2",
                    >= 50 => "N3",
                    >= 35 => "N4",
                    >= 20 => "N5",
                    _ => "Below N5"
                };
            }

            return overallScore >= 70 ? "Advanced" : overallScore >= 50 ? "Intermediate" : "Beginner";
        }

        //private List<string> ExtractStrengths(List<VoiceAssessmentQuestion> questions, int count)
        //{
        //    var strengths = new List<string>();
        //    foreach (var q in questions)
        //    {
        //        if (q.EvaluationResult?.Strengths != null)
        //            strengths.AddRange(q.EvaluationResult.Strengths);
        //    }

        //    var distinct = strengths.Distinct().Take(5).ToList();
        //    if (!distinct.Any())
        //    {
        //        distinct.Add($"Đã hoàn thành {count} câu hỏi");
        //    }
        //    return distinct;
        //}

        //private List<string> ExtractImprovements(List<VoiceAssessmentQuestion> questions, int completed, int total)
        //{
        //    var improvements = new List<string>();
        //    foreach (var q in questions)
        //    {
        //        if (q.EvaluationResult?.AreasForImprovement != null)
        //            improvements.AddRange(q.EvaluationResult.AreasForImprovement);
        //    }

        //    var distinct = improvements.Distinct().Take(5).ToList();

        //    if (completed < total)
        //    {
        //        distinct.Insert(0, $"Hoàn thành thêm {total - completed} câu để có đánh giá chính xác hơn");
        //    }

        //    return distinct.Any() ? distinct : new List<string> { "Luyện tập thêm để cải thiện" };
        //}

        private string GetNextLevelRequirement(string currentLevel, string languageName)
        {
            if (languageName.Contains("Anh"))
            {
                return currentLevel switch
                {
                    "A1" => "Để đạt A2: Học 500-700 từ mới, luyện past tense, cải thiện fluency lên 80-100 wpm",
                    "A2" => "Để đạt B1: Học 1000+ từ, master all tenses, luyện speaking 100-120 wpm",
                    "B1" => "Để đạt B2: Vocabulary 3500+, advanced grammar, fluency 120-140 wpm",
                    "B2" => "Để đạt C1: Sophisticated vocabulary, complex structures, near-native fluency",
                    "C1" => "Để đạt C2: Native-like proficiency in all aspects",
                    _ => "Hoàn thành đánh giá đầy đủ để biết yêu cầu cụ thể"
                };
            }
            else if (languageName.Contains("Trung"))
            {
                return currentLevel switch
                {
                    "HSK 1" => "Để đạt HSK 2: Học thêm 300 từ, master 4 thanh, luyện tập hội thoại đơn giản",
                    "HSK 2" => "Để đạt HSK 3: Học 600+ từ mới, cải thiện độ chính xác thanh điệu lên 80%+",
                    "HSK 3" => "Để đạt HSK 4: Vocabulary 1200-2500 từ, chengyu cơ bản, fluency tốt",
                    "HSK 4" => "Để đạt HSK 5: 2500+ từ, chengyu nâng cao, đọc báo Trung Quốc",
                    "HSK 5" => "Để đạt HSK 6: 5000+ từ, văn học cổ điển, thành ngữ native",
                    _ => "Hoàn thành đánh giá đầy đủ để biết yêu cầu cụ thể"
                };
            }
            else if (languageName.Contains("Nhật"))
            {
                return currentLevel switch
                {
                    "N5" => "Để đạt N4: Học 700+ từ mới, 200 kanji, master て-form và basic conjugations",
                    "N4" => "Để đạt N3: 1500+ từ mới, 350 kanji, cải thiện pitch accent, keigo cơ bản",
                    "N3" => "Để đạt N2: 3000+ từ, 650 kanji, business Japanese, advanced grammar",
                    "N2" => "Để đạt N1: 6000+ từ, 1000+ kanji, literary forms, native-like keigo",
                    _ => "Hoàn thành đánh giá đầy đủ để biết yêu cầu cụ thể"
                };
            }

            return "Hoàn thành bài test để nhận lộ trình học tập chi tiết";
        }

        private string GetNextLevelForLanguage(string currentLevel, string languageName)
        {
            if (languageName.Contains("Anh"))
            {
                return currentLevel switch
                {
                    "A1" => "A2",
                    "A2" => "B1",
                    "B1" => "B2",
                    "B2" => "C1",
                    "C1" => "C2",
                    _ => "A2"
                };
            }
            else if (languageName.Contains("Trung"))
            {
                return currentLevel switch
                {
                    "HSK 1" => "HSK 2",
                    "HSK 2" => "HSK 3",
                    "HSK 3" => "HSK 4",
                    "HSK 4" => "HSK 5",
                    "HSK 5" => "HSK 6",
                    _ => "HSK 2"
                };
            }
            else if (languageName.Contains("Nhật"))
            {
                return currentLevel switch
                {
                    "N5" => "N4",
                    "N4" => "N3",
                    "N3" => "N2",
                    "N2" => "N1",
                    _ => "N4"
                };
            }

            return "Intermediate";
        }

        private List<string> GetDefaultVocalTips(string languageName)
        {
            return new List<string>
    {
        $"Luyện phát âm {languageName} 15-20 phút mỗi ngày",
        "Ghi âm giọng nói để tự đánh giá",
        "Bắt chước phát âm của người bản ngữ",
        "Thực hành đọc to với tốc độ phù hợp",
        "Tham gia các nhóm speaking practice online"
    };
        }
        private string CleanJsonResponse(string response)
        {
            var cleaned = response.Trim();

            // Remove markdown code blocks
            if (cleaned.StartsWith("```json"))
            {
                cleaned = cleaned.Replace("```json", "").Replace("```", "").Trim();
            }
            else if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned.Replace("```", "").Trim();
            }

            // Find JSON boundaries
            var jsonStart = cleaned.IndexOf('{');
            var jsonEnd = cleaned.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                return cleaned.Substring(jsonStart, jsonEnd - jsonStart);
            }

            return cleaned;
        }
        public async Task<List<VoiceAssessmentQuestion>> GenerateVoiceAssessmentQuestionsAsync(
            string languageCode,
            string languageName)
        {
            try
            {
                _logger.LogInformation("🚀 Starting GenerateVoiceAssessmentQuestionsAsync for {LanguageCode}", languageCode);

             
                _logger.LogInformation("🇻🇳 Using Vietnamese-supported fallback questions for {LanguageCode}", languageCode);

                var questions = GetFallbackVoiceQuestionsWithVietnamese(languageCode, languageName);

             
                foreach (var question in questions)
                {
                    _logger.LogInformation("✅ Question {Number}: Vietnamese={HasVietnamese}, WordGuides={WordCount}",
                        question.QuestionNumber,
                        !string.IsNullOrEmpty(question.VietnameseTranslation),
                        question.WordGuides?.Count ?? 0);
                }

                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GenerateVoiceAssessmentQuestionsAsync");
                return GetFallbackVoiceQuestionsWithVietnamese(languageCode, languageName);
            }
        }
        public async Task<BatchVoiceEvaluationResult> EvaluateBatchVoiceResponsesAsync(
    List<VoiceAssessmentQuestion> questions,
    string languageCode,
    string languageName)
        {
            try
            {
                _logger.LogInformation("🎯 Starting BATCH voice evaluation for {Count} questions in {Language}",
                    questions.Count, languageName);

                var prompt = BuildBatchVoiceEvaluationPrompt(questions, languageCode, languageName);

                // Prepare multipart request with multiple audio files
                var parts = new List<object> { new { text = prompt } };

                foreach (var question in questions.Where(q => !q.IsSkipped && !string.IsNullOrEmpty(q.AudioFilePath)))
                {
                    var audioBytes = await File.ReadAllBytesAsync(question.AudioFilePath);
                    var base64Audio = Convert.ToBase64String(audioBytes);

                    parts.Add(new
                    {
                        inline_data = new
                        {
                            mime_type = "audio/mp3",
                            data = base64Audio
                        }
                    });

                    // Add separator text
                    parts.Add(new { text = $"[Audio for Question {question.QuestionNumber}]" });
                }

                var requestBody = new
                {
                    contents = new[]
                    {
                new { parts = parts.ToArray() }
            },
                    generationConfig = new
                    {
                        temperature = 0.3,
                        maxOutputTokens = 4096,
                        topP = 0.8,
                        topK = 10,
                        response_mime_type = "application/json"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var model = "gemini-2.0-flash-exp"; // Use latest model
                var url = $"{_settings.BaseUrl}/models/{model}:generateContent?key={_settings.ApiKey}";

                _logger.LogInformation("Calling Gemini API for batch evaluation...");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API error: {StatusCode} - {Content}",
                        response.StatusCode, responseContent);
                    throw new HttpRequestException($"Gemini API returned {response.StatusCode}");
                }

                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                var resultText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";

                _logger.LogInformation("✅ Received batch evaluation response: {Length} characters", resultText.Length);

                return ParseBatchEvaluationResponse(resultText, languageName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch voice evaluation");
                return CreateFallbackBatchEvaluation(questions, languageName);
            }
        }

        private string BuildBatchVoiceEvaluationPrompt(
            List<VoiceAssessmentQuestion> questions,
            string languageCode,
            string languageName)
        {
            var completedQuestions = questions.Where(q => !q.IsSkipped).ToList();
            var standardName = GetStandardName(languageCode);

            var questionDetails = string.Join("\n", completedQuestions.Select(q => $@"
### Câu {q.QuestionNumber}: {q.Difficulty.ToUpper()}
**Yêu cầu**: {q.PromptText}
**Từ bắt buộc phải nói**: {string.Join(", ", q.WordGuides.Select(w => w.Word))}
**Nghĩa tiếng Việt**: {q.VietnameseTranslation}
"));

            return $@"# Đánh giá Speaking {languageName} - Batch Evaluation

## Thông tin:
- Tổng số câu: {completedQuestions.Count}/{questions.Count}
- Tiêu chuẩn: {standardName}
- Ngôn ngữ: {languageName} ({languageCode})

## Danh sách câu hỏi:
{questionDetails}

## YÊU CẦU ĐÁNH GIÁ:

### 1. ACCURACY (40%) - Quan trọng nhất
- Kiểm tra người dùng có NÓI ĐỦ TẤT CẢ từ bắt buộc không?
- Nếu thiếu từ → giảm điểm mạnh
- Nếu nói sai từ → điểm 0 cho từ đó

### 2. PRONUNCIATION (30%)
- Phát âm từng từ chính xác
- Trọng âm đúng vị trí
- Ngữ điệu tự nhiên

### 3. FLUENCY (20%)
- Tốc độ nói phù hợp
- Ít ngập ngừng
- Liền mạch

### 4. GRAMMAR (10%)
- Cấu trúc câu đúng
- Thì động từ chính xác

## ĐỊNH DẠNG OUTPUT (JSON):

{{
  ""overallLevel"": ""{GetExampleLevel(languageCode)}"",
  ""overallScore"": 75,
  ""questionResults"": [
    {{
      ""questionNumber"": 1,
      ""spokenWords"": [""hello"", ""world""],
      ""missingWords"": [""beautiful""],
      ""accuracyScore"": 67,
      ""pronunciationScore"": 80,
      ""fluencyScore"": 70,
      ""grammarScore"": 75,
      ""feedback"": ""Thiếu từ 'beautiful'. Phát âm 'hello' tốt nhưng 'world' cần cải thiện trọng âm.""
    }}
  ],
  ""strengths"": [
    ""Phát âm rõ ràng các nguyên âm"",
    ""Tốc độ nói ổn định"",
    ""Tự tin khi nói""
  ],
  ""weaknesses"": [
    ""Thiếu 2/3 từ vựng yêu cầu ở câu 1"",
    ""Ngữ pháp câu 3 sai thì quá khứ"",
    ""Trọng âm từ 'important' chưa đúng""
  ],
  ""recommendedCourses"": [
    {{
      ""focus"": ""Vocabulary Building"",
      ""reason"": ""Cần mở rộng vốn từ vựng cơ bản"",
      ""level"": ""Beginner""
    }},
    {{
      ""focus"": ""Pronunciation Practice"",
      ""reason"": ""Cải thiện trọng âm và ngữ điệu"",
      ""level"": ""Elementary""
    }},
    {{
      ""focus"": ""Grammar Fundamentals"",
      ""reason"": ""Ôn luyện các thì cơ bản"",
      ""level"": ""Beginner""
    }}
  ]
}}

**LƯU Ý QUAN TRỌNG**:
- ACCURACY là tiêu chí quan trọng nhất - phải check kỹ từng từ
- spokenWords: chỉ list từ người dùng thực sự nói ra
- missingWords: list từ bắt buộc nhưng người dùng không nói
- Feedback phải cụ thể, chỉ ra từng lỗi rõ ràng
- Strengths/Weaknesses phải dựa trên dữ liệu thực tế
- recommendedCourses: 2-3 khóa học cụ thể, có lý do rõ ràng

Chỉ trả về JSON, không thêm markdown hay giải thích.";
        }

        private BatchVoiceEvaluationResult ParseBatchEvaluationResponse(string response, string languageName)
        {
            try
            {
                var cleanedResponse = CleanJsonResponse(response);

                var result = JsonSerializer.Deserialize<BatchVoiceEvaluationResult>(cleanedResponse,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });

                if (result != null)
                {
                    result.EvaluatedAt = DateTime.UtcNow;
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing batch evaluation response");
            }

            return CreateFallbackBatchEvaluation(new List<VoiceAssessmentQuestion>(), languageName);
        }

        private BatchVoiceEvaluationResult CreateFallbackBatchEvaluation(
            List<VoiceAssessmentQuestion> questions,
            string languageName)
        {
            return new BatchVoiceEvaluationResult
            {
                OverallLevel = "Unassessed",
                OverallScore = 0,
                QuestionResults = questions.Select(q => new QuestionEvaluationResult
                {
                    QuestionNumber = q.QuestionNumber,
                    SpokenWords = new List<string>(),
                    MissingWords = q.WordGuides.Select(w => w.Word).ToList(),
                    AccuracyScore = 0,
                    PronunciationScore = 0,
                    FluencyScore = 0,
                    GrammarScore = 0,
                    Feedback = "Không thể đánh giá bằng AI. Vui lòng thử lại."
                }).ToList(),
                Strengths = new List<string> { "Đã hoàn thành bài test" },
                Weaknesses = new List<string> { "Cần đánh giá lại bằng AI" },
                RecommendedCourses = new List<CourseRecommendation>
        {
            new() { Focus = "General Practice", Reason = "Luyện tập tổng quát", Level = "Beginner" }
        },
                EvaluatedAt = DateTime.UtcNow
            };
        }

        private string GetLanguageName(string languageCode)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => "tiếng Anh",
                "ZH" => "tiếng Trung",
                "JP" => "tiếng Nhật",
                _ => "ngôn ngữ"
            };
        }

        private string GetLanguageStandards(string languageCode)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => @"
**English Standards (CEFR):**
- **Beginner (A1)**: Basic phrases, simple present tense, 500-1000 words
- **Elementary (A2)**: Simple conversations, past tense, 1000-2000 words
- **Intermediate (B1-B2)**: Complex sentences, all tenses, 2000-4000 words  
- **Advanced (C1-C2)**: Native-like fluency, complex grammar, 8000+ words

**Speaking Assessment Criteria:**
- Clear pronunciation and word stress
- Natural intonation patterns
- Appropriate speaking pace
- Grammar accuracy in speech
- Vocabulary range and precision",

                "ZH" => @"
**Chinese Standards (HSK):**
- **Beginner (HSK 1-2)**: Pinyin mastery, 300-600 characters, basic tones
- **Elementary (HSK 3)**: 600-900 characters, simple conversations
- **Intermediate (HSK 4-5)**: 1200-2500 characters, complex sentences
- **Advanced (HSK 6)**: 2500+ characters, idioms, cultural expressions

**Speaking Assessment Criteria:**
- Accurate tone production (4 tones + neutral)
- Clear initials and finals pronunciation
- Natural rhythm and flow
- Proper use of measure words
- Cultural appropriateness",

                "JP" => @"
**Japanese Standards (JLPT):**
- **Beginner (N5-N4)**: Hiragana/Katakana, 300-600 kanji, basic grammar
- **Elementary (N3)**: 650-1000 kanji, intermediate grammar patterns
- **Intermediate (N2)**: 1000+ kanji, advanced grammar, keigo basics
- **Advanced (N1)**: 2000+ kanji, native-level expressions, complex keigo

**Speaking Assessment Criteria:**
- Correct pitch accent patterns
- Proper mora timing
- Accurate long vowel pronunciation
- Appropriate politeness levels (keigo)
- Natural sentence endings",

                _ => "Standard language proficiency levels: Beginner, Elementary, Intermediate, Advanced"
            };
        }

        private VoiceEvaluationResult CreateFallbackVoiceEvaluation()
        {
            return new VoiceEvaluationResult
            {
                OverallScore = 70,
                Pronunciation = new PronunciationScore
                {
                    Score = 70,
                    Level = "Fair",
                    Feedback = "Cần đánh giá thủ công để có kết quả chính xác hơn",
                    MispronuncedWords = new List<string>()
                },
                Fluency = new FluencyScore
                {
                    Score = 70,
                    SpeakingRate = 120,
                    PauseCount = 5,
                    Rhythm = "Average",
                    Feedback = "Cần đánh giá thủ công để có kết quả chính xác hơn"
                },
                Grammar = new GrammarScore
                {
                    Score = 70,
                    GrammarErrors = new List<string>(),
                    StructureAssessment = "Average",
                    Feedback = "Cần đánh giá thủ công để có kết quả chính xác hơn"
                },
                Vocabulary = new VocabularyScore
                {
                    Score = 70,
                    RangeAssessment = "Good",
                    AccuracyAssessment = "Fair",
                    Feedback = "Cần đánh giá thủ công để có kết quả chính xác hơn"
                },
                DetailedFeedback = "Không thể đánh giá tự động bằng AI. Vui lòng thử lại hoặc liên hệ support.",
                Strengths = new List<string> { "Đã hoàn thành bài test voice", "Tích cực tham gia đánh giá" },
                AreasForImprovement = new List<string> { "Cần đánh giá chi tiết hơn từ AI", "Thử lại với file audio chất lượng tốt hơn" }
            };
        }

        private VoiceEvaluationResult ParseVoiceEvaluationResponse(string response)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(response))
                {
                    _logger.LogWarning("Received empty response from Gemini API - using fallback");
                    return CreateFallbackVoiceEvaluation();
                }

                var cleanedResponse = CleanJsonResponse(response);
                if (string.IsNullOrWhiteSpace(cleanedResponse))
                {
                    _logger.LogWarning("Cleaned response is empty - using fallback");
                    return CreateFallbackVoiceEvaluation();
                }

                var jsonDoc = JsonDocument.Parse(cleanedResponse);
                

              
                if (jsonDoc.RootElement.TryGetProperty("isCorrectLanguage", out var isCorrectLang) &&
                    !isCorrectLang.GetBoolean())
                {
                    var detectedLang = jsonDoc.RootElement.TryGetProperty("detectedLanguage", out var detectedProp)
                        ? detectedProp.GetString() : "Unknown";
                    var expectedLang = jsonDoc.RootElement.TryGetProperty("expectedLanguage", out var expectedProp)
                        ? expectedProp.GetString() : "Unknown";
                    var errorMessage = jsonDoc.RootElement.TryGetProperty("message", out var msgProp)
                        ? msgProp.GetString() : "Ngôn ngữ không đúng";

                    _logger.LogError("🌍 LANGUAGE MISMATCH: Expected {Expected}, Detected {Detected}",
                        expectedLang, detectedLang);

                    return new VoiceEvaluationResult
                    {
                        OverallScore = 0,
                        Pronunciation = new PronunciationScore
                        {
                            Score = 0,
                            Level = "Language Error",
                            Feedback = $"❌ Ngôn ngữ không đúng: {errorMessage}",
                            MispronuncedWords = new List<string>()
                        },
                        Fluency = new FluencyScore
                        {
                            Score = 0,
                            SpeakingRate = 0,
                            PauseCount = 0,
                            Rhythm = "Error",
                            Feedback = "Ngôn ngữ audio không khớp với assessment"
                        },
                        Grammar = new GrammarScore
                        {
                            Score = 0,
                            GrammarErrors = new List<string>(),
                            StructureAssessment = "Error",
                            Feedback = "Không thể đánh giá - sai ngôn ngữ"
                        },
                        Vocabulary = new VocabularyScore
                        {
                            Score = 0,
                            RangeAssessment = "Error",
                            AccuracyAssessment = "Error",
                            Feedback = "Không thể đánh giá - sai ngôn ngữ"
                        },
                        DetailedFeedback = $"🚨 **Lỗi ngôn ngữ**: {errorMessage}\n\n" +
                                         $"**Phát hiện**: {detectedLang}\n" +
                                         $"**Yêu cầu**: {expectedLang}\n\n" +
                                         $"Vui lòng ghi âm lại bằng đúng ngôn ngữ của bài test.",
                        Strengths = new List<string>(),
                        AreasForImprovement = new List<string>
                {
                    $"Ghi âm bằng đúng ngôn ngữ ({expectedLang})",
                    "Kiểm tra lại ngôn ngữ assessment trước khi ghi âm",
                    $"Bài test này yêu cầu {expectedLang}, không phải {detectedLang}"
                }
                    };
                }

                
                var evaluation = JsonSerializer.Deserialize<VoiceEvaluationResult>(cleanedResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                return evaluation ?? CreateFallbackVoiceEvaluation();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing voice evaluation response");
                return CreateFallbackVoiceEvaluation();
            }
        }


        private List<VoiceAssessmentQuestion> GetFallbackVoiceQuestionsWithVietnamese(string languageCode, string languageName)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => new List<VoiceAssessmentQuestion>
        {
          
            new() {
                QuestionNumber = 1,
                Question = "Hãy phát âm rõ ràng từ cơ bản sau:",
                PromptText = "Hello",
                VietnameseTranslation = "Xin chào",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "Hello",
                        Pronunciation = "/həˈloʊ/ (hơ-lô)",
                        VietnameseMeaning = "Xin chào",
                        Example = "Hello! Nice to meet you."
                    }
                },
                QuestionType = "single_word",
                Difficulty = "beginner",
                MaxRecordingSeconds = 15
            },
         
            new() {
                QuestionNumber = 2,
                Question = "Hãy phát âm rõ ràng 2 từ trung bình sau:",
                PromptText = "Beautiful - Important",
                VietnameseTranslation = "Đẹp - Quan trọng",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "Beautiful",
                        Pronunciation = "/ˈbjuːtɪfl/ (bíu-ti-fồ)",
                        VietnameseMeaning = "Đẹp",
                        Example = "What a beautiful day!"
                    },
                    new() {
                        Word = "Important",
                        Pronunciation = "/ɪmˈpɔːrtnt/ (im-pót-tờnt)",
                        VietnameseMeaning = "Quan trọng",
                        Example = "Education is very important."
                    }
                },
                QuestionType = "two_words",
                Difficulty = "intermediate",
                MaxRecordingSeconds = 20
            },
         
            new() {
                QuestionNumber = 3,
                Question = "Hãy phát âm rõ ràng 3 từ khó sau:",
                PromptText = "Pronunciation - Magnificent - Extraordinary",
                VietnameseTranslation = "Phát âm - Tráng lệ - Phi thường",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "Pronunciation",
                        Pronunciation = "/prəˌnʌnsiˈeɪʃn/ (prơ-nần-si-ây-sần)",
                        VietnameseMeaning = "Phát âm",
                        Example = "Good pronunciation is essential."
                    },
                    new() {
                        Word = "Magnificent",
                        Pronunciation = "/mæɡˈnɪfɪsnt/ (mạg-ní-fi-sờnt)",
                        VietnameseMeaning = "Tráng lệ, lộng lẫy",
                        Example = "The view is absolutely magnificent."
                    },
                    new() {
                        Word = "Extraordinary",
                        Pronunciation = "/ɪkˈstrɔːrdneri/ (ik-xờ-trờ-đi-ne-ri)",
                        VietnameseMeaning = "Phi thường, đặc biệt",
                        Example = "She has extraordinary talent."
                    }
                },
                QuestionType = "three_words",
                Difficulty = "advanced",
                MaxRecordingSeconds = 30
            },
           
            new() {
                QuestionNumber = 4,
                Question = "Hãy đọc câu dài sau với ngữ điệu và nhịp điệu tự nhiên:",
                PromptText = "Technology has revolutionized the way we communicate and learn in the modern world.",
                VietnameseTranslation = "Công nghệ đã cách mạng hóa cách chúng ta giao tiếp và học tập trong thế giới hiện đại.",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "revolutionized",
                        Pronunciation = "/ˌrevəˈluːʃənaɪzd/ (re-vơ-lú-sần-naizd)",
                        VietnameseMeaning = "Cách mạng hóa",
                        Example = "The internet revolutionized communication."
                    },
                    new() {
                        Word = "communicate",
                        Pronunciation = "/kəˈmjuːnɪkeɪt/ (kơ-miu-ni-kết)",
                        VietnameseMeaning = "Giao tiếp",
                        Example = "We communicate through various channels."
                    },
                    new() {
                        Word = "modern",
                        Pronunciation = "/ˈmɑːdərn/ (mó-đờn)",
                        VietnameseMeaning = "Hiện đại",
                        Example = "We live in a modern society."
                    }
                },
                QuestionType = "long_sentence",
                Difficulty = "advanced",
                MaxRecordingSeconds = 45
            }
        },

                "ZH" => new List<VoiceAssessmentQuestion>
        {
        
            new() {
                QuestionNumber = 1,
                Question = "请准确发音下列基础词汇:",
                PromptText = "你好",
                VietnameseTranslation = "Xin chào",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "你好",
                        Pronunciation = "nǐ hǎo (ni hảo )",
                        VietnameseMeaning = "Xin chào",
                        Example = "你好！很高兴见到你。(Xin chào! Rất vui được gặp bạn)"
                    }
                },
                QuestionType = "single_word",
                Difficulty = "beginner",
                MaxRecordingSeconds = 15
            },
           
            new() {
                QuestionNumber = 2,
                Question = "请准确发音下列中等词汇:",
                PromptText = "美丽 - 重要",
                VietnameseTranslation = "Đẹp - Quan trọng",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "美丽",
                        Pronunciation = "měi lì (mẻi li )",
                        VietnameseMeaning = "Đẹp",
                        Example = "这里的风景很美丽。(Phong cảnh ở đây rất đẹp)"
                    },
                    new() {
                        Word = "重要",
                        Pronunciation = "zhòng yào (trọng diệu )",
                        VietnameseMeaning = "Quan trọng",
                        Example = "教育非常重要。(Giáo dục rất quan trọng)"
                    }
                },
                QuestionType = "two_words",
                Difficulty = "intermediate",
                MaxRecordingSeconds = 20
            },
       
            new() {
                QuestionNumber = 3,
                Question = "请准确发音下列高难度词汇:",
                PromptText = "发音 - 壮丽 - 非凡",
                VietnameseTranslation = "Phát âm - Tráng lệ - Phi thường",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "发音",
                        Pronunciation = "fā yīn (pha in )",
                        VietnameseMeaning = "Phát âm",
                        Example = "正确的发音很重要。(Phát âm đúng rất quan trọng)"
                    },
                    new() {
                        Word = "壮丽",
                        Pronunciation = "zhuàng lì (tráng lệ)",
                        VietnameseMeaning = "Tráng lệ",
                        Example = "山景非常壮丽。(Cảnh núi rất tráng lệ)"
                    },
                    new() {
                        Word = "非凡",
                        Pronunciation = "fēi fán (phi phàm )",
                        VietnameseMeaning = "Phi thường",
                        Example = "她有非凡的才能。(Cô ấy có tài năng phi thường)"
                    }
                },
                QuestionType = "three_words",
                Difficulty = "advanced",
                MaxRecordingSeconds = 30
            },
          
            new() {
                QuestionNumber = 4,
                Question = "请以自然的语调和节奏朗读下列长句:",
                PromptText = "科技已经彻底改变了我们在现代世界中交流和学习的方式。",
                VietnameseTranslation = "Công nghệ đã cách mạng hóa cách chúng ta giao tiếp và học tập trong thế giới hiện đại.",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "彻底改变",
                        Pronunciation = "chè dǐ gǎi biàn (triệt để cải biến ",
                        VietnameseMeaning = "Thay đổi hoàn toàn",
                        Example = "这个发明彻底改变了生活。(Phát minh này thay đổi hoàn toàn cuộc sống)"
                    },
                    new() {
                        Word = "交流",
                        Pronunciation = "jiāo liú (giao lưu )",
                        VietnameseMeaning = "Giao tiếp",
                        Example = "我们需要更多交流。(Chúng ta cần giao tiếp nhiều hơn)"
                    },
                    new() {
                        Word = "现代世界",
                        Pronunciation = "xiàn dài shì jiè (hiện đại thế giới)",
                        VietnameseMeaning = "Thế giới hiện đại",
                        Example = "现代世界变化很快。(Thế giới hiện đại thay đổi rất nhanh)"
                    }
                },
                QuestionType = "long_sentence",
                Difficulty = "advanced",
                MaxRecordingSeconds = 45
            }
        },

                "JP" => new List<VoiceAssessmentQuestion>
        {
          
            new() {
                QuestionNumber = 1,
                Question = "次の基本的な単語を正確に発音してください:",
                PromptText = "こんにちは",
                VietnameseTranslation = "Xin chào",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "こんにちは",
                        Pronunciation = "konnichiwa (kon-ni-chi-oa)",
                        VietnameseMeaning = "Xin chào (ban ngày)",
                        Example = "こんにちは、元気ですか。(Xin chào, bạn khỏe không?)"
                    }
                },
                QuestionType = "single_word",
                Difficulty = "beginner",
                MaxRecordingSeconds = 15
            },
  
            new() {
                QuestionNumber = 2,
                Question = "次の中級レベルの単語を正確に発音してください:",
                PromptText = "美しい - 大切",
                VietnameseTranslation = "Đẹp - Quan trọng",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "美しい (うつくしい)",
                        Pronunciation = "utsukushii (u-xu-ku-xì-i)",
                        VietnameseMeaning = "Đẹp",
                        Example = "桜はとても美しいです。(Hoa anh đào rất đẹp)"
                    },
                    new() {
                        Word = "大切 (たいせつ)",
                        Pronunciation = "taisetsu (tai-xe-xu)",
                        VietnameseMeaning = "Quan trọng",
                        Example = "家族はとても大切です。(Gia đình rất quan trọng)"
                    }
                },
                QuestionType = "two_words",
                Difficulty = "intermediate",
                MaxRecordingSeconds = 20
            },
          
            new() {
                QuestionNumber = 3,
                Question = "次の上級レベルの単語を正確に発音してください:",
                PromptText = "発音 - 素晴らしい - 特別",
                VietnameseTranslation = "Phát âm - Tuyệt vời - Đặc biệt",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "発音 (はつおん)",
                        Pronunciation = "hatsuon (ha-xu-on)",
                        VietnameseMeaning = "Phát âm",
                        Example = "正しい発音は大切です。(Phát âm đúng rất quan trọng)"
                    },
                    new() {
                        Word = "素晴らしい (すばらしい)",
                        Pronunciation = "subarashii (xu-ba-ra-xì-i)",
                        VietnameseMeaning = "Tuyệt vời",
                        Example = "素晴らしい景色ですね。(Cảnh này tuyệt vời nhỉ)"
                    },
                    new() {
                        Word = "特別 (とくべつ)",
                        Pronunciation = "tokubetsu (to-ku-be-xu)",
                        VietnameseMeaning = "Đặc biệt",
                        Example = "今日は特別な日です。(Hôm nay là ngày đặc biệt)"
                    }
                },
                QuestionType = "three_words",
                Difficulty = "advanced",
                MaxRecordingSeconds = 30
            },
        
            new() {
                QuestionNumber = 4,
                Question = "次の長い文を自然なイントネーションとリズムで読んでください:",
                PromptText = "技術は現代世界における私たちのコミュニケーションと学習の方法を革命的に変えました。",
                VietnameseTranslation = "Công nghệ đã cách mạng hóa cách chúng ta giao tiếp và học tập trong thế giới hiện đại.",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "革命的 (かくめいてき)",
                        Pronunciation = "kakumeiteki (ka-ku-mei-te-ki)",
                        VietnameseMeaning = "Mang tính cách mạng",
                        Example = "革命的な変化が起きました。(Đã xảy ra thay đổi mang tính cách mạng)"
                    },
                    new() {
                        Word = "コミュニケーション",
                        Pronunciation = "komyunikeeshon (ko-myu-ni-kê-syon)",
                        VietnameseMeaning = "Giao tiếp",
                        Example = "コミュニケーションは重要です。(Giao tiếp rất quan trọng)"
                    },
                    new() {
                        Word = "現代世界 (げんだいせかい)",
                        Pronunciation = "gendai sekai (gen-dai xe-kai)",
                        VietnameseMeaning = "Thế giới hiện đại",
                        Example = "現代世界は複雑です。(Thế giới hiện đại rất phức tạp)"
                    }
                },
                QuestionType = "long_sentence",
                Difficulty = "advanced",
                MaxRecordingSeconds = 45
            }
        },

                "VI" => new List<VoiceAssessmentQuestion>
        {
            // 📍 Câu 1: NÓI 1 TỪ CỞ BẢN  
            new() {
                QuestionNumber = 1,
                Question = "Hãy phát âm rõ ràng từ cơ bản sau:",
                PromptText = "Xin chào",
                VietnameseTranslation = "Xin chào",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "Xin chào",
                        Pronunciation = "sin tʂàːw (sin chào)",
                        VietnameseMeaning = "Lời chào hỏi",
                        Example = "Xin chào! Rất vui được gặp bạn."
                    }
                },
                QuestionType = "single_word",
                Difficulty = "beginner",
                MaxRecordingSeconds = 15
            },
            // 📍 Câu 2: NÓI 2 TỪ TRUNG BÌNH
            new() {
                QuestionNumber = 2,
                Question = "Hãy phát âm rõ ràng 2 từ trung bình sau:",
                PromptText = "Xinh đẹp - Quan trọng",
                VietnameseTranslation = "Xinh đẹp - Quan trọng",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "Xinh đẹp",
                        Pronunciation = "siɲ ɗɛ́p (sinh đếp)",
                        VietnameseMeaning = "Có vẻ đẹp hấp dẫn",
                        Example = "Cô ấy rất xinh đẹp."
                    },
                    new() {
                        Word = "Quan trọng",
                        Pronunciation = "kwaːn ʈɔ̀ŋ (quan trọng)",
                        VietnameseMeaning = "Có ý nghĩa lớn",
                        Example = "Giáo dục rất quan trọng."
                    }
                },
                QuestionType = "two_words",
                Difficulty = "intermediate",
                MaxRecordingSeconds = 20
            },
            // 📍 Câu 3: NÓI 3 TỪ KHÓ
            new() {
                QuestionNumber = 3,
                Question = "Hãy phát âm rõ ràng 3 từ khó sau:",
                PromptText = "Phát âm - Tráng lệ - Phi thường",
                VietnameseTranslation = "Phát âm - Tráng lệ - Phi thường",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "Phát âm",
                        Pronunciation = "faːt ʔaːm (phát âm)",
                        VietnameseMeaning = "Cách nói ra âm thanh",
                        Example = "Phát âm chuẩn rất quan trọng."
                    },
                    new() {
                        Word = "Tráng lệ",
                        Pronunciation = "ʈaːŋ lɛ̂ (tráng lệ)",
                        VietnameseMeaning = "Hùng vĩ, lộng lẫy",
                        Example = "Cung điện rất tráng lệ."
                    },
                    new() {
                        Word = "Phi thường",
                        Pronunciation = "fi tʰɨ̀ːəŋ (phi thường)",
                        VietnameseMeaning = "Khác thường, đặc biệt",
                        Example = "Cậu ấy có tài năng phi thường."
                    }
                },
                QuestionType = "three_words",
                Difficulty = "advanced",
                MaxRecordingSeconds = 30
            },
            // 📍 Câu 4: NÓI 1 CÂU DÀI
            new() {
                QuestionNumber = 4,
                Question = "Hãy đọc câu dài sau với ngữ điệu và nhịp điệu tự nhiên:",
                PromptText = "Công nghệ đã cách mạng hóa cách chúng ta giao tiếp và học tập trong thế giới hiện đại.",
                VietnameseTranslation = "Công nghệ đã cách mạng hóa cách chúng ta giao tiếp và học tập trong thế giới hiện đại.",
                WordGuides = new List<WordWithGuide>
                {
                    new() {
                        Word = "Cách mạng hóa",
                        Pronunciation = "kaːk maːŋ hoaː (cách mạng hóa)",
                        VietnameseMeaning = "Thay đổi một cách căn bản",
                        Example = "Internet đã cách mạng hóa truyền thông."
                    },
                    new() {
                        Word = "Giao tiếp",
                        Pronunciation = "zaːw tiɛ́p (giao tiếp)",
                        VietnameseMeaning = "Trao đổi thông tin",
                        Example = "Giao tiếp hiệu quả rất cần thiết."
                    },
                    new() {
                        Word = "Hiện đại",
                        Pronunciation = "hiɛ̂n ɗaːj (hiện đại)",
                        VietnameseMeaning = "Thuộc về thời đại bây giờ",
                        Example = "Chúng ta sống trong xã hội hiện đại."
                    }
                },
                QuestionType = "long_sentence",
                Difficulty = "advanced",
                MaxRecordingSeconds = 45
            }
        },

                _ => new List<VoiceAssessmentQuestion>()
            };
        }
        private List<VoiceAssessmentQuestion> GetFallbackVoiceQuestions(string languageCode, string languageName)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => new List<VoiceAssessmentQuestion>
        {
            new() {
                QuestionNumber = 1,
                Question = "Hãy đọc to các từ sau với phát âm rõ ràng:",
                PromptText = "Hello - World - Beautiful",
                QuestionType = "pronunciation",
                Difficulty = "beginner",
                MaxRecordingSeconds = 30
            },
            new() {
                QuestionNumber = 2,
                Question = "Hãy giới thiệu bản thân trong 60 giây:",
                PromptText = "Please introduce yourself. Tell me your name, age, where you're from, what you do, and what you like to do in your free time. Speak clearly and naturally.",
                QuestionType = "speaking",
                Difficulty = "elementary",
                MaxRecordingSeconds = 60
            },
            new() {
                QuestionNumber = 3,
                Question = "Mô tả một ngày làm việc/học tập điển hình của bạn:",
                PromptText = "Describe your typical workday or study day from morning to evening. What time do you wake up? What do you do? How do you feel about your daily routine?",
                QuestionType = "speaking",
                Difficulty = "intermediate",
                MaxRecordingSeconds = 90
            },
            new() {
                QuestionNumber = 4,
                Question = "Thảo luận về tác động của công nghệ đối với giáo dục:",
                PromptText = "What do you think about the impact of technology on education? Discuss both positive and negative effects. Give specific examples and explain your personal opinion.",
                QuestionType = "speaking",
                Difficulty = "advanced",
                MaxRecordingSeconds = 120
            }
        },
                "ZH" => new List<VoiceAssessmentQuestion>
        {
            new() {
                QuestionNumber = 1,
                Question = "请读出下列词语，注意发音和声调:",
                PromptText = "你好 - 世界 - 美丽 ",
                QuestionType = "pronunciation",
                Difficulty = "beginner",
                MaxRecordingSeconds = 30
            },
            new() {
                QuestionNumber = 2,
                Question = "请用中文介绍一下你自己:",
                PromptText = "请介绍你的姓名、年龄、来自哪里、职业以及你的兴趣爱好。请说得清楚一些。",
                QuestionType = "speaking",
                Difficulty = "elementary",
                MaxRecordingSeconds = 60
            },
            new() {
                QuestionNumber = 3,
                Question = "描述一下你的家乡和那里的文化:",
                PromptText = "请描述你家乡的天气、食物、文化和你最喜欢的地方。你觉得你的家乡有什么特色？",
                QuestionType = "speaking",
                Difficulty = "intermediate",
                MaxRecordingSeconds = 90
            },
            new() {
                QuestionNumber = 4,
                Question = "谈谈你对现代科技的看法:",
                PromptText = "请谈谈现代科技对我们生活的影响，包括好处和坏处。你认为科技发展对教育有什么影响？",
                QuestionType = "speaking",
                Difficulty = "advanced",
                MaxRecordingSeconds = 120
            }
        },
                "JP" => new List<VoiceAssessmentQuestion>
        {
            new() {
                QuestionNumber = 1,
                Question = "次の単語を読んでください:",
                PromptText = "こんにちは - せかい - うつくしい ",
                QuestionType = "pronunciation",
                Difficulty = "beginner",
                MaxRecordingSeconds = 30
            },
            new() {
                QuestionNumber = 2,
                Question = "自己紹介をしてください:",
                PromptText = "お名前、年齢、出身地、お仕事、趣味について話してください。はっきりと話してください。",
                QuestionType = "speaking",
                Difficulty = "elementary",
                MaxRecordingSeconds = 60
            },
            new() {
                QuestionNumber = 3,
                Question = "好きな季節について話してください:",
                PromptText = "好きな季節とその理由、その季節にすること、季節の食べ物などについて説明してください。",
                QuestionType = "speaking",
                Difficulty = "intermediate",
                MaxRecordingSeconds = 90
            },
            new() {
                QuestionNumber = 4,
                Question = "日本の文化について意見を述べてください:",
                PromptText = "日本の文化で興味深いと思うことについて、具体例を挙げて説明してください。他の国の文化と比較してもいいです。",
                QuestionType = "speaking",
                Difficulty = "advanced",
                MaxRecordingSeconds = 120
            }
        },
                _ => new List<VoiceAssessmentQuestion>()
            };
        }

        private string BuildVoiceAssessmentPrompt(string languageCode, string languageName)
        {
            var standardFramework = GetStandardFramework(languageCode);
            var vietnameseGuidelines = GetVietnameseGuidelinesByLanguage(languageCode);

            return $@"# Tạo bộ câu hỏi đánh giá giọng nói {languageName} với hướng dẫn tiếng Việt

## Yêu cầu:
Tạo 4 câu hỏi đánh giá khả năng nói {languageName}, độ khó tăng dần.

## Tiêu chuẩn {languageName}:
{standardFramework}

## YÊU CẦU BẮT BUỘC - Vietnamese Support:
{vietnameseGuidelines}

## Format trả về (JSON):
{{
    ""questions"": [
        {{
            ""questionNumber"": 1,
            ""question"": ""Hãy đọc to các từ sau với phát âm rõ ràng:"",
            ""promptText"": ""Hello - World - Beautiful"",
            ""vietnameseTranslation"": ""Xin chào - Thế giới - Đẹp"",
            ""wordGuides"": [
                {{
                    ""word"": ""Hello"",
                    ""pronunciation"": ""/həˈloʊ/ (hơ-lô)"",
                    ""vietnameseMeaning"": ""Xin chào"",
                    ""example"": ""Hello, how are you?""
                }},
                {{
                    ""word"": ""World"",
                    ""pronunciation"": ""/wɜːrld/ (uớt)"",
                    ""vietnameseMeaning"": ""Thế giới"",
                    ""example"": ""Welcome to the world""
                }},
                {{
                    ""word"": ""Beautiful"",
                    ""pronunciation"": ""/ˈbjuːtɪfl/ (bíu-ti-fồ)"",
                    ""vietnameseMeaning"": ""Đẹp, xinh đẹp"",
                    ""example"": ""What a beautiful day!""
                }}
            ],
            ""questionType"": ""pronunciation"",
            ""difficulty"": ""beginner"",
            ""maxRecordingSeconds"": 30
        }},
        {{
            ""questionNumber"": 2,
            ""question"": ""Hãy giới thiệu bản thân trong 60 giây:"",
            ""promptText"": ""Please introduce yourself. Tell me your name, age, where you're from, and your hobbies."",
            ""vietnameseTranslation"": ""Vui lòng giới thiệu bản thân. Nói cho tôi biết tên, tuổi, quê quán và sở thích của bạn."",
            ""wordGuides"": [
                {{
                    ""word"": ""introduce"",
                    ""pronunciation"": ""/ˌɪntrəˈduːs/ (in-trơ-diúc)"",
                    ""vietnameseMeaning"": ""Giới thiệu"",
                    ""example"": ""Let me introduce myself""
                }},
                {{
                    ""word"": ""hobbies"",
                    ""pronunciation"": ""/ˈhɑːbiz/ (há-biz)"",
                    ""vietnameseMeaning"": ""Sở thích"",
                    ""example"": ""My hobbies are reading and swimming""
                }}
            ],
            ""questionType"": ""speaking"",
            ""difficulty"": ""elementary"",
            ""maxRecordingSeconds"": 60
        }}
    ]
}}

**LƯU Ý QUAN TRỌNG**:
- LUÔN LUÔN cung cấp wordGuides cho tất cả từ quan trọng
- Phiên âm gồm cả IPA và phiên âm dễ đọc bằng tiếng Việt (trong ngoặc)
- Ví dụ phải đơn giản, dễ hiểu
- vietnameseTranslation phải chính xác và tự nhiên";
        }
        private string GetVietnameseGuidelinesByLanguage(string languageCode)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => @"
### Hướng dẫn tiếng Việt cho English:
1. **Phiên âm**: IPA + phiên âm Việt hóa dễ đọc
   - VD: /həˈloʊ/ (hơ-lô)
   - Chú ý: th (thờ), r (rờ/ờ), w (u), v (vờ)

2. **Từ vựng cần có wordGuides**:
   - Câu 1 (Beginner): Tất cả các từ trong promptText
   - Câu 2 (Elementary): Từ khóa quan trọng (5-8 từ)
   - Câu 3 (Intermediate): Từ khó, cụm từ (3-5 từ)
   - Câu 4 (Advanced): Từ vựng academic, idioms (3-5 từ)

3. **Ví dụ**: Câu ngắn, thực tế, dễ nhớ",

                "ZH" => @"
### Hướng dẫn tiếng Việt cho Chinese:
1. **Phiên âm**: Pinyin + cách đọc Việt hóa
   - VD: nǐ hǎo (ni hảo) - thanh 3 = giọng hỏi
   - Chú ý 4 thanh: 1-ngang, 2-sắc, 3-hỏi, 4-nặng

2. **Hán tự + Pinyin + Nghĩa**:
   - 你好 (nǐ hǎo / ni hảo) - Xin chào
   - 世界 (shì jiè / sự giế) - Thế giới

3. **Giải thích thanh điệu**: 
   - Thanh 1 (¯): Giọng ngang, cao
   - Thanh 2 (´): Giọng đi lên (như hỏi ""hả?"")
   - Thanh 3 (ˇ): Giọng hỏi, xuống rồi lên
   - Thanh 4 (`): Giọng nặng, đi xuống mạnh",

                "JP" => @"
### Hướng dẫn tiếng Việt cho Japanese:
1. **Phiên âm**: Romaji + cách đọc Việt hóa
   - VD: こんにちは (konnichiwa / kon-ni-chi-oa) - Xin chào
   - Chú ý: chi (chi không phải ki), tsu (xu), fu (fu không phải hu)

2. **Kanji + Hiragana/Katakana + Romaji + Nghĩa**:
   - 世界 (せかい / sekai / xê-kai) - Thế giới
   - 美しい (うつくしい / utsukushii / u-xu-ku-xì-i) - Đẹp

3. **Pitch accent** (optional cho advanced):
   - ⓪ Low-High: Âm đầu thấp, sau cao
   - ① High-Low: Âm đầu cao, sau thấp",

                _ => "Cung cấp phiên âm và nghĩa tiếng Việt cho tất cả từ khóa"
            };
        }

        private class VoiceQuestionsResponse
        {
            public List<VoiceAssessmentQuestion>? Questions { get; set; }
        }
        private class TeacherQualificationAiResponse
        {
            public List<string>? SuggestedTeachingLevels { get; set; }
            public int ConfidenceScore { get; set; }
            public string? ReasoningExplanation { get; set; }
            public List<QualificationAssessmentAi>? QualificationAssessments { get; set; }
            public string? OverallRecommendation { get; set; }
            public List<TeachingLevelSuggestionAi>? TeachingLevelSuggestions { get; set; }
        }

        private class QualificationAssessmentAi
        {
            public string? CredentialName { get; set; }
            public string? CredentialType { get; set; }
            public int RelevanceScore { get; set; }
            public string? Assessment { get; set; }
            public List<string>? SupportedLevels { get; set; }
        }

        private class TeachingLevelSuggestionAi
        {
            public string? Level { get; set; }
            public int ConfidenceScore { get; set; }
            public string? Justification { get; set; }
            public bool IsRecommended { get; set; }
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
