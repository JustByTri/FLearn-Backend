using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class AIFeedBack
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AIFeedbackId { get; set; }
        public Guid? ConversationID { get; set; }
        [ForeignKey(nameof(ConversationID))]
        public virtual Conversation Conversation { get; set; }
        [Required]
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
