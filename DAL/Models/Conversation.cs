using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Conversation
    {
        [Key]
        public Guid ConversationID { get; set; }
        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }
        [Required]
        public Guid LanguageId { get; set; }
        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; } // Fetch conversation based on active language id of user
        public Guid? TopicId { get; set; }
        [ForeignKey(nameof(TopicId))]
        public virtual Topic Topic { get; set; }
        public Guid? ExerciseSubmissionId { get; set; }
        [ForeignKey(nameof(ExerciseSubmissionId))]
        public virtual ExerciseSubmission? ExerciseSubmission { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual AIFeedBack AIFeedBack { get; set; }
    }
}
