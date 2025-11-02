using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class ConversationMessage
    {
        [Key]
        public Guid ConversationMessageID { get; set; }
        [Required]
        public Guid ConversationSessionID { get; set; }
        [ForeignKey(nameof(ConversationSessionID))]
        public virtual ConversationSession ConversationSession { get; set; } = null!;
        [Required]
        public MessageSender Sender { get; set; } // User hoặc AI
        [Required]
        [StringLength(2000)]
        public string MessageContent { get; set; } = string.Empty;
        public MessageType MessageType { get; set; } = MessageType.Text;
        // Audio message (nếu user gửi voice)
        public string? AudioUrl { get; set; }
        public string? AudioPublicId { get; set; }
        public int? AudioDuration { get; set; } // seconds
        public int SequenceOrder { get; set; } // Thứ tự tin nhắn
        public DateTime SentAt { get; set; }
        // Response time (thời gian user suy nghĩ trước khi trả lời)
        public int? ResponseTimeMs { get; set; }
    }
}
