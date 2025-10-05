using Common.DTO.Assement;
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

        
        Task<List<VoiceAssessmentQuestion>> GenerateVoiceAssessmentQuestionsAsync(string languageCode, string languageName);
        Task<VoiceEvaluationResult> EvaluateVoiceResponseDirectlyAsync(VoiceAssessmentQuestion question, IFormFile audioFile, string languageCode);
        Task<VoiceAssessmentResultDto> GenerateVoiceAssessmentResultAsync(
     string languageCode,
     string languageName,
     List<VoiceAssessmentQuestion> questions,
     string? goalName = null);
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
}
