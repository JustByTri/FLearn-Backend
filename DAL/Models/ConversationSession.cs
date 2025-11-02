using DAL.Helpers;
using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class ConversationSession
    {
        [Key]
        public Guid ConversationSessionID { get; set; }
        [Required]
        public Guid LearnerId { get; set; }
        [ForeignKey(nameof(LearnerId))]
        public virtual LearnerLanguage Learner { get; set; } = null!;
        [Required]
        public Guid TopicID { get; set; }
        [ForeignKey(nameof(TopicID))]
        public virtual Topic Topic { get; set; } = null!;
        [Required]
        public Guid GlobalPromptID { get; set; }
        [ForeignKey(nameof(GlobalPromptID))]
        public virtual GlobalConversationPrompt GlobalPrompt { get; set; } = null!;
        [Required]
        public Guid LanguageId { get; set; }
        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; } = null!;
        [Required]
        [StringLength(50)]
        public string DifficultyLevel { get; set; } = string.Empty; // A1, N5, HSK1...
        [Required]
        [StringLength(200)]
        public string SessionName { get; set; } = string.Empty;
        [StringLength(1000)]
        public string? GeneratedScenario { get; set; } // Tình huống do AI tạo ra
        [StringLength(500)]
        public string? AICharacterRole { get; set; } // Vai trò AI được gán
        [StringLength(2000)]
        public string? GeneratedSystemPrompt { get; set; } // System prompt cụ thể cho session này
        public ConversationSessionStatus Status { get; set; } = ConversationSessionStatus.Active;
        public int MessageCount { get; set; } = 0;
        public int Duration { get; set; } = 0;
        public float? OverallScore { get; set; }
        public float? FluentScore { get; set; }
        public float? GrammarScore { get; set; }
        public float? VocabularyScore { get; set; }
        public float? CulturalScore { get; set; }
        [StringLength(2000)]
        public string? AIFeedback { get; set; }
        [StringLength(1000)]
        public string? Improvements { get; set; }
        [StringLength(1000)]
        public string? Strengths { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public virtual ICollection<ConversationTask> Tasks { get; set; } = new List<ConversationTask>();
        public virtual ICollection<ConversationMessage> ConversationMessages { get; set; } = new List<ConversationMessage>();
    }
}

