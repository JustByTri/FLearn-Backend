using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Roadmap
    {
        [Key]
        public Guid RoadmapID { get; set; }
        [Required]
        [ForeignKey("LearnerLanguage")]
        public Guid LearnerLanguageId { get; set; }
        public virtual LearnerLanguage LearnerLanguage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public virtual ICollection<RoadmapDetail> RoadmapDetails { get; set; } = new List<RoadmapDetail>();
    }
}
