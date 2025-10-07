using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Roadmap
    {
        [Key]
        public Guid RoadmapID { get; set; }
        [Required]
        public Guid LearnerId { get; set; } // UserId + LanguageId
        public virtual LearnerLanguage LearnerLanguage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public virtual ICollection<RoadmapDetail> RoadmapDetails { get; set; } = new List<RoadmapDetail>();
    }
}
