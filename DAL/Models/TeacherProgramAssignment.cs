using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class TeacherProgramAssignment
    {
        [Key]
        public Guid ProgramAssignmentId { get; set; }
        [Required]
        public Guid TeacherId { get; set; }
        [ForeignKey(nameof(TeacherId))]
        public virtual TeacherProfile Teacher { get; set; } = null!;
        [Required]
        public Guid ProgramId { get; set; }
        [ForeignKey(nameof(ProgramId))]
        public virtual Program Program { get; set; } = null!;
        [Required]
        public Guid LevelId { get; set; }
        [ForeignKey(nameof(LevelId))]
        public virtual Level Level { get; set; } = null!;
        public bool Status { get; set; } = true;
        public DateTime AssignedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? ModifiedAt { get; set; }
    }
}
