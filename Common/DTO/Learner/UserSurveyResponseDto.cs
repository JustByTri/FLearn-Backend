using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Learner
{
    public class UserSurveyResponseDto
    {
        public Guid SurveyID { get; set; }

        // ❌ REMOVED: LearningGoal - AI sẽ tự động xác định

        public string CurrentLevel { get; set; } = string.Empty;

        public Guid PreferredLanguageID { get; set; }

        public string PreferredLanguageName { get; set; } = string.Empty;



        public string LearningReason { get; set; } = string.Empty;

        public string PreviousExperience { get; set; } = string.Empty;

        public string PreferredLearningStyle { get; set; } = string.Empty;

        public string InterestedTopics { get; set; } = string.Empty;

        public string PrioritySkills { get; set; } = "Speaking";

        public string TargetTimeline { get; set; } = string.Empty;

        // ✅ NEW: Speaking-specific fields
        public string SpeakingChallenges { get; set; } = string.Empty;

        public int? ConfidenceLevel { get; set; }

        public string PreferredAccent { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; }

        // AI Generated Recommendations
        public AiCourseRecommendationDto? AiRecommendations { get; set; }
    }
}


