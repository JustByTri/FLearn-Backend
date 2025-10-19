using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class TeacherClass
    {
        [Key]
        public Guid ClassID { get; set; }

        [ForeignKey("Teacher")]
        public Guid TeacherID { get; set; }

        [ForeignKey("Language")]
        public Guid LanguageID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        public DateTime EndDateTime { get; set; }

        [Required]
        public int MinStudents { get; set; }

        [Required]
        public int Capacity { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerStudent { get; set; }

        [StringLength(500)]
        public string GoogleMeetLink { get; set; }

        [Required]
        public ClassStatus Status { get; set; } = ClassStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User Teacher { get; set; }
        public virtual Language Language { get; set; }
        public virtual ICollection<ClassEnrollment> Enrollments { get; set; }
        public virtual ICollection<ClassDispute> Disputes { get; set; }
        public virtual ICollection<TeacherPayout> Payouts { get; set; }
    
        [NotMapped]
        public int CurrentEnrollments => Enrollments?.Count(e => e.Status == EnrollmentStatus.Paid) ?? 0;


    }

    public enum ClassStatus
    {
        Draft = 0,
        Scheduled = 1,
        Published = 2,
        InProgress = 3,
        Finished = 4,
        Completed_PendingPayout = 5,
        Cancelled_InsufficientStudents = 6,
        Cancelled_TeacherUnavailable = 7,
        Cancelled_Other = 8,
        Completed_Paid = 9,
        Cancelled =10
    }
}
