using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class ExerciseEvaluationDetail
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public Guid SubmissionId { get; set; }
        [ForeignKey(nameof(SubmissionId))]
        public virtual ExerciseSubmission Submission { get; set; }
        [Required]
        public EvaluationCriterion Criterion { get; set; }
        [Required]
        public EvaluationType EvaluatedBy { get; set; } // AI hoặc Teacher
        [Range(0, 100)]
        public double Score { get; set; }
        [StringLength(500)]
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
