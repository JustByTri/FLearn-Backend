using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Exercise
    {
        [Key]
        public Guid ExerciseID { get; set; }

        [StringLength(500)]
        public string Hints { get; set; }

        [Range(1, 10, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int Position { get; set; }

        [StringLength(1000)]
        public string Materials { get; set; }

        [StringLength(1000)]
        public string ExpectedAnswer { get; set; }

        [StringLength(1000)]
        public string Prompt { get; set; }

        [Required]
        public Guid LessonID { get; set; }

        public Lesson? Lesson { get; set; }

        public enum ExerciseType
        {
            None,
            MultipleChoice,
            FillInTheBlank,
            Speaking
        }

        [Required]
        public ExerciseType Type { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        [Required]
        public string Title { get; set; }

        public string Content { get; set; }
    }
}
