using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Message
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MessageId { get; set; }
        public Guid ConversationId { get; set; }
        [ForeignKey(nameof(ConversationId))]
        public virtual Conversation Conversation { get; set; }
        public string Role { get; set; } = string.Empty; // "user" or "AI"
        public string Content { get; set; } = string.Empty;
        public string? AudioUrl { get; set; } // URL to the audio file if applicable
        public string? AudioPublicId { get; set; } // Public ID for audio file management (e.g., Cloudinary)
        public DateTime SentAt { get; set; }
    }
}
