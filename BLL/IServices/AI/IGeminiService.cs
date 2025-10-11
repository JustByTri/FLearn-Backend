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

        Task<BatchVoiceEvaluationResult> EvaluateBatchVoiceResponsesAsync(
   List<VoiceAssessmentQuestion> questions,
   string languageCode,
   string languageName);
        Task<List<VoiceAssessmentQuestion>> GenerateVoiceAssessmentQuestionsAsync(string languageCode, string languageName);
        Task<VoiceEvaluationResult> EvaluateVoiceResponseDirectlyAsync(VoiceAssessmentQuestion question, IFormFile audioFile, string languageCode);
      
        Task<GeneratedConversationContentDto> GenerateConversationContentAsync(ConversationContextDto context);
        Task<string> GenerateResponseAsync(string systemPrompt, string userMessage, List<string> conversationHistory);
        Task<ConversationEvaluationResult> EvaluateConversationAsync(string evaluationPrompt);
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
    public class ConversationEvaluationResult
    {
        public float OverallScore { get; set; }
        public float FluentScore { get; set; }
        public float GrammarScore { get; set; }
        public float VocabularyScore { get; set; }
        public float CulturalScore { get; set; }
        public string AIFeedback { get; set; } = string.Empty;
        public string Improvements { get; set; } = string.Empty;
        public string Strengths { get; set; } = string.Empty;
    }
}
