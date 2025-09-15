using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Conversation
    {
        [Key]
        public Guid ConversationID { get; set; }

        [Required]
        public Guid UserID { get; set; }
        public User? User { get; set; }
        public Language? Language { get; set; }
        public Guid LanguageID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public Guid AIFeedBackID { get; set; }
        public ICollection<AIFeedBack> AIFeedBacks { get; set; }
        public ICollection<Recording> Recordings { get; set; }

        [StringLength(200)]
        public string Topic { get; set; }
    }
}
