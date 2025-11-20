using Common.DTO.Assement;
using Common.DTO.Conversation;
using Common.DTO.Learner;
using Common.DTO.Teacher;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices.AI
{
    public interface IGeminiService
    {

        Task<AiCourseRecommendationDto> GenerateCourseRecommendationsAsync(
            UserSurveyResponseDto survey,
            List<CourseInfoDto> availableCourses);

        Task<string> GenerateStudyPlanAsync(UserSurveyResponseDto survey);
        Task<List<string>> GenerateStudyTipsAsync(UserSurveyResponseDto survey);


        Task<TeacherQualificationAnalysisDto> AnalyzeTeacherQualificationsAsync(
            TeacherApplicationDto application,
            List<TeacherCredentialDto> credentials);

        /// <summary>
        /// Đánh giá hàng loạt câu trả lời bằng giọng nói
        /// </summary>
        /// <param name="programLevelNames">Thang đo (ví dụ: ["A1", "A2", "B1"]) để AI trả về kết quả</param>
        Task<BatchVoiceEvaluationResult> EvaluateBatchVoiceResponsesAsync(
            List<VoiceAssessmentQuestion> questions,
            string languageCode,
            string languageName,
            List<string>? programLevelNames = null); 

        /// <summary>
        /// Tạo câu hỏi đánh giá giọng nói
        /// </summary>
        /// <param name="programName">Tên của khung chương trình (ví dụ: "Tiếng Anh Giao Tiếp")</param>
        Task<List<VoiceAssessmentQuestion>> GenerateVoiceAssessmentQuestionsAsync(
            string languageCode,
            string languageName,
            string? programName = null); 

        Task<VoiceEvaluationResult> EvaluateVoiceResponseDirectlyAsync(VoiceAssessmentQuestion question, IFormFile audioFile, string languageCode);

        Task<GeneratedConversationContentDto> GenerateConversationContentAsync(ConversationContextDto context);
        Task<RoleplayResponseDto> GenerateResponseAsync(
            string systemPrompt,
            string userMessage,
            List<string> conversationHistory,
            string languageName = "English",
            string topic = "",
            string aiRoleName = "AI Partner", string level = "A1");
        Task<ConversationEvaluationResult> EvaluateConversationAsync(string evaluationPrompt, string targetLanguage);

        // NEW: Gợi ý từ đồng nghĩa theo trình độ
        Task<SynonymSuggestionDto> GenerateSynonymSuggestionsAsync(string userMessage, string targetLanguage, string currentLevel);

        Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage);
        IAsyncEnumerable<string> GenerateResponseStreamAsync(
    string systemPrompt,
    string userMessage,
    List<string> history);
    }

    public class CourseInfoDto
    {
        public Guid CourseID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public List<string> Topics { get; set; } = new();
        public List<string> Skills { get; set; } = new();
        public int Duration { get; set; }
        public string Difficulty { get; set; } = string.Empty;
    }
    
    // UPDATED: Đánh giá chi tiết hơn, không set điểm cứng
    // NOTE: Sử dụng classes từ Common.DTO.Conversation cho DetailedSkillAnalysis và SpecificObservation
    public class ConversationEvaluationResult
    {
        // Giữ lại điểm tổng quan (để tương thích)
        public float OverallScore { get; set; }
        
        // Đánh giá chi tiết theo từng khía cạnh
        public DetailedSkillAnalysis? FluentAnalysis { get; set; }
        public DetailedSkillAnalysis? GrammarAnalysis { get; set; }
        public DetailedSkillAnalysis? VocabularyAnalysis { get; set; }
        public DetailedSkillAnalysis? CulturalAnalysis { get; set; }
        
        // Giữ lại các trường cũ để backwards compatibility
        public float FluentScore { get; set; }
        public float GrammarScore { get; set; }
        public float VocabularyScore { get; set; }
        public float CulturalScore { get; set; }
        
        public string AIFeedback { get; set; } = string.Empty;
        public string Improvements { get; set; } = string.Empty;
        public string Strengths { get; set; } = string.Empty;
        
        // NEW: Phân tích sâu hơn
        public List<SpecificObservation>? SpecificObservations { get; set; }
        public List<string>? PositivePatterns { get; set; }
        public List<string>? AreasNeedingWork { get; set; }
        public string? ProgressSummary { get; set; }
    }
}
