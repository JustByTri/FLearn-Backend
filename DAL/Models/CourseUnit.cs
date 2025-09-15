using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class CourseUnit
    {
        [Key]
        public Guid CourseUnitID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Range(1, 100, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int Position { get; set; }

        [Required]
        public Guid CourseID { get; set; }

        public Course? Course { get; set; }
        public ICollection<Lesson> Lessons { get; set; }
    }
}
