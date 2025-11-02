using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class ConversationTask
    {
        [Key]
        public Guid TaskID { get; set; }
        [Required]
        public Guid ConversationSessionID { get; set; }
        [ForeignKey(nameof(ConversationSessionID))]
        public virtual ConversationSession ConversationSession { get; set; } = null!;
        [Required]
        [StringLength(500)]
        public string TaskDescription { get; set; } = string.Empty; // Description of the task to be performed
        [StringLength(1000)]
        public string? TaskContext { get; set; } // Additional context for the task
        public int TaskSequence { get; set; } // Order of tasks
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
        public bool IsCompleted { get; set; } = false;
        [StringLength(500)]
        public string? CompletionNotes { get; set; }
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
    }
}
