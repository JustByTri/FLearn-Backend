using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Type;

namespace DAL.Models
{
    public class ConversationSession
    {
        [Key]
        public Guid ConversationSessionID { get; set; }

        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        [Required]
        public Guid LanguageId { get; set; }
        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; }

        [Required]
        public Guid TopicID { get; set; }
        [ForeignKey(nameof(TopicID))]
        public virtual Topic Topic { get; set; }

        [Required]
        public Guid GlobalPromptID { get; set; }
        [ForeignKey(nameof(GlobalPromptID))]
        public virtual GlobalConversationPrompt GlobalPrompt { get; set; }

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

        // AI Evaluation Results
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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<ConversationMessage>? ConversationMessages { get; set; } = new List<ConversationMessage>();
    }
}

