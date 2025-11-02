using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class CourseTopic
    {
        [Key]
        public Guid CourseTopicID { get; set; }
        [Required]
        public Guid CourseID { get; set; }
        [ForeignKey(nameof(CourseID))]
        public virtual Course Course { get; set; } = null!;
        [Required]
        public Guid TopicID { get; set; }
        [ForeignKey(nameof(TopicID))]
        public virtual Topic Topic { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
