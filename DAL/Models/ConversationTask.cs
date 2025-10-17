using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class ConversationTask
    {
        [Key]
        public Guid TaskID { get; set; }

        [Required]
        public Guid ConversationSessionID { get; set; }
        [ForeignKey(nameof(ConversationSessionID))]
        public virtual ConversationSession ConversationSession { get; set; }

        [Required]
        [StringLength(500)]
        public string TaskDescription { get; set; }

        [StringLength(1000)]
        public string? TaskContext { get; set; } // Additional context for the task

        public int TaskSequence { get; set; } // Order of tasks

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed

        public bool IsCompleted { get; set; } = false;

        [StringLength(500)]
        public string? CompletionNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
