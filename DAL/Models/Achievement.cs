using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Achievement
    {
        [Key]
        public Guid AchievementID { get; set; }
        [Required]
        public Guid LanguageId { get; set; }
        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; }
        [Required]
        [StringLength(200)]
        public string Title { get; set; }
        [StringLength(500)]
        public string Description { get; set; }
        public string? IconUrl { get; set; }
        public string? Criteria { get; set; }
        public bool Status { get; set; } = true; // Active by default or false for inactive
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public virtual ICollection<LearnerAchievement>? LearnerAchievements { get; set; } = new List<LearnerAchievement>();
    }
}
