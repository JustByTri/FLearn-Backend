using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Enrollment
    {
        [Key]
        public Guid EnrollmentID { get; set; }

        [Required]
        public Guid UserID { get; set; }

        [Required]
        public Guid CourseID { get; set; }

        [Required]
        public DateTime EnrolledAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [Range(0, 1)]
        public decimal Progress { get; set; }

        public bool? IsActive { get; set; }
    }
}
