using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Learner
{
    public class UserSurveyDto
    {
        // ❌ REMOVED: LearningGoal - AI sẽ tự động xác định

        [Required(ErrorMessage = "Trình độ hiện tại là bắt buộc")]
        public string CurrentLevel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngôn ngữ muốn học là bắt buộc")]
        public Guid PreferredLanguageID { get; set; }

        // ❌ REMOVED: DailyStudyTime - Không cần thiết cho speaking platform

        [Required(ErrorMessage = "Lý do học speaking là bắt buộc")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Lý do học phải từ 10 đến 500 ký tự")]
        public string LearningReason { get; set; } = string.Empty;

        public string PreviousExperience { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phương pháp học ưa thích là bắt buộc")]
        public string PreferredLearningStyle { get; set; } = string.Empty;

        public string InterestedTopics { get; set; } = string.Empty;

   
        public string PrioritySkills { get; set; } = "Speaking";

        [Required(ErrorMessage = "Thời hạn mục tiêu là bắt buộc")]
        public string TargetTimeline { get; set; } = string.Empty;

 
        public string SpeakingChallenges { get; set; } = string.Empty;

        [Range(1, 10, ErrorMessage = "Mức độ tự tin phải từ 1 đến 10")]
        public int? ConfidenceLevel { get; set; }

        public string PreferredAccent { get; set; } = string.Empty;
    }
}


