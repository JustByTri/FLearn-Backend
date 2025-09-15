using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Course
    {
        [Key]
        public Guid CourseID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        [StringLength(300)]
        public string CoverImageUrl { get; set; }

        [Required]
        [Range(0, 100000)]
        public decimal Price { get; set; }

        [Required]
        public Guid TeacherID { get; set; }

        public User? Teacher { get; set; }
        public Language? Language { get; set; }

        [Required]
        public Guid LanguageID { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        [StringLength(500)]
        public string Goal { get; set; }

        [StringLength(50)]
        public string Level { get; set; }

        [StringLength(100)]
        public string SkillFocus { get; set; }

        public DateTime? PublishedAt { get; set; }

        [Range(0, 10000)]
        public int NumLessons { get; set; }

        public Guid ApprovedByID { get; set; }

        public enum CourseStatus
        {
            Draft,
            PendingApproval,
            Published,
            Rejected,
            Archived
        }
        [Required]
        public CourseStatus Status { get; set; }

        public ICollection<CourseUnit> CourseUnits { get; set; }
        public ICollection<CourseTopic> CourseTopics { get; set; }
        public ICollection<PurchasesDetail> PurchasesDetails { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<CourseSubmission> CourseSubmissions { get; set; }
    }
}
