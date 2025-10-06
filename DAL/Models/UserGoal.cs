using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class UserGoal
    {
        [Key]
        public Guid UserGoalID { get; set; }

        [Required]
        public Guid UserID { get; set; }

        [ForeignKey("UserID")]
        public User User { get; set; } = null!;

        [Required]
        public Guid LanguageID { get; set; }

        [ForeignKey("LanguageID")]
        public Language Language { get; set; } = null!;

       
        public int? GoalId { get; set; }

        [ForeignKey(nameof(GoalId))]
        public Goal? Goal { get; set; }
        /// <summary>
        /// Level hiện tại
        /// </summary>
        
        [StringLength(20)]
        public string? DeterminedLevel { get; set; } 

        [Range(0, 100)]
        public int? OverallScore { get; set; }

     
        [Column(TypeName = "TEXT")]
        public string? RoadmapData { get; set; } 

       
        [Column(TypeName = "TEXT")]
        public string? RecommendedCoursesData { get; set; } 

        public bool HasCompletedSurvey { get; set; } = false;
        public bool HasSkippedSurvey { get; set; } = false;
        public bool HasCompletedVoiceAssessment { get; set; } = false;

   
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SurveyCompletedAt { get; set; }
        public DateTime? VoiceAssessmentCompletedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsVoiceAssessmentPending { get; set; } = false;
        public DateTime? VoiceAssessmentPendingAt { get; set; }

        // Pending Voice Assessment Data (JSON)
        [Column(TypeName = "TEXT")]
        public string? PendingRoadmapData { get; set; }

        [Column(TypeName = "TEXT")]
        public string? PendingRecommendedCoursesData { get; set; }

        [StringLength(20)]
        public string? PendingDeterminedLevel { get; set; }

        [Range(0, 100)]
        public int? PendingOverallScore { get; set; }


        [StringLength(1000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
