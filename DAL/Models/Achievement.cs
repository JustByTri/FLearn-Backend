using DAL.Helpers;
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
        public virtual Language Language { get; set; } = null!;
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;
        [StringLength(500)]
        public string Description { get; set; } = null!;
        public string? IconUrl { get; set; }
        public string? Criteria { get; set; }
        public bool Status { get; set; } = true;
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public virtual ICollection<LearnerAchievement> LearnerAchievements { get; set; } = new List<LearnerAchievement>();
    }
}
