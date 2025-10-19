using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class ClassEnrollment
    {
        [Key]
        public Guid EnrollmentID { get; set; }

        [ForeignKey("Class")]
        public Guid ClassID { get; set; }

        [ForeignKey("Student")]
        public Guid StudentID { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal AmountPaid { get; set; }

        [StringLength(100)]
        public string PaymentTransactionId { get; set; }

        [Required]
        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual TeacherClass Class { get; set; }
        public virtual User Student { get; set; }
        public virtual ICollection<ClassDispute> Disputes { get; set; }
    }

    public enum EnrollmentStatus
    {
        Pending = 0,
        Paid = 1,
        Refunded = 2,
        Cancelled = 3,
        Completed = 4
    }
}

