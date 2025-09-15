using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Lesson
    {
        [Key]
        public Guid LessonID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(2000)]
        public string Content { get; set; }
        [Required]
        [Range(1, 10, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int Position { get; set; }

        [Required]
        [StringLength(100)]
        public string SkillFocus { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public DateTime IsPublished { get; set; }

        [Required]
        public Guid CourseUnitID { get; set; }

        public CourseUnit? CourseUnit { get; set; }

        [Required]
        public DateTime CreateAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        public ICollection<Exercise> Exerices { get; set; }
    }
}
