using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class RoadmapDetail
    {
        [Key]
        public Guid RoadmapDetailID { get; set; }
        [Required]
        public Guid RoadmapID { get; set; }
        [ForeignKey(nameof(RoadmapID))]
        public virtual Roadmap Roadmap { get; set; }
        [Required]
        public Guid CourseId { get; set; }
        [ForeignKey(nameof(CourseId))]
        public virtual Course Course { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}