using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class TeacherPayout
    {
        [Key]
        public Guid TeacherPayoutId { get; set; }
        [Required]
        public Guid TeacherId { get; set; }
        public virtual TeacherProfile Teacher { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalLessons { get; set; } = 0;
        public double TotalEarnings { get; set; } = 0;
        public double RejectedPenalty { get; set; } = 0;
        public double FinalAmount { get; set; } = 0;
        [Required]
        public Guid StaffId { get; set; }
        [ForeignKey("Class")]
        public Guid? ClassID { get; set; }

        public virtual TeacherClass Class { get; set; }
        public virtual StaffLanguage Staff { get; set; }
        public TeacherPayoutStatus Status { get; set; } = TeacherPayoutStatus.Pending;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
