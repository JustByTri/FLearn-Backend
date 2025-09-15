using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class CourseSubmission
    {
        [Key]
        public Guid CourseSubmissionID { get; set; }

        [Required]
        public Guid SubmittedBy { get; set; }

        public User? Submitter { get; set; }

        [Required]
        public DateTime SubmittedAt { get; set; }

        [Required]
        public Guid CourseID { get; set; }

        public Course? Course { get; set; }

        public enum SubmissionStatus
        {
            Pending,
            Approved,
            Rejected
        }
        [Required]
        public SubmissionStatus Status { get; set; }

        public Guid ReviewBy { get; set; }

        [StringLength(1000)]
        public string ReviewComment { get; set; }

        public DateTime? ReviewedAt { get; set; }
    }
}
