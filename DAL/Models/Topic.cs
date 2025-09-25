using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Topic
    {
        [Key]
        public Guid TopicID { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? PublicId { get; set; }
        public virtual ICollection<CourseTopic> CourseTopics { get; set; } = new List<CourseTopic>();
    }
}
