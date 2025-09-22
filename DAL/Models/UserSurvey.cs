using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class UserSurvey
    {
        [Key]
        public Guid SurveyID { get; set; }

        [Required]
        public Guid UserID { get; set; }

        [ForeignKey("UserID")]
        public User User { get; set; } = null!;

        // ❌ REMOVED: LearningGoal - AI sẽ tự động xác định

        // Trình độ hiện tại
        [Required]
        [StringLength(50)]
        public string CurrentLevel { get; set; } = string.Empty;

        // Ngôn ngữ muốn học
        [Required]
        public Guid PreferredLanguageID { get; set; }

        [ForeignKey("PreferredLanguageID")]
        public Language PreferredLanguage { get; set; } = null!;

        // ❌ REMOVED: DailyStudyTime - Không cần thiết cho speaking platform

        // Lý do học speaking
        [Required]
        [StringLength(500)]
        public string LearningReason { get; set; } = string.Empty;

        // Kinh nghiệm học ngôn ngữ trước đó
        [StringLength(300)]
        public string PreviousExperience { get; set; } = string.Empty;

        // Phương pháp học ưa thích cho speaking
        [Required]
        [StringLength(200)]
        public string PreferredLearningStyle { get; set; } = string.Empty;

        // Chủ đề quan tâm để nói
        [StringLength(300)]
        public string InterestedTopics { get; set; } = string.Empty;


        [Required]
        [StringLength(100)]
        public string PrioritySkills { get; set; } = "Speaking";

        // Mục tiêu thời gian để đạt được
        [Required]
        [StringLength(50)]
        public string TargetTimeline { get; set; } = string.Empty;

        // ✅ NEW: Thêm speaking-specific fields
        [StringLength(200)]
        public string SpeakingChallenges { get; set; } = string.Empty;

        [Range(1, 10)]
        public int? ConfidenceLevel { get; set; }

        [StringLength(200)]
        public string PreferredAccent { get; set; } = string.Empty;

        public bool IsCompleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        // AI Generated Recommendations (JSON)
        [Column(TypeName = "TEXT")]
        public string? AiRecommendations { get; set; }
    }
}


