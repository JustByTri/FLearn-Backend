using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
  public class GlobalConversationPrompt
    {

        [Key]
        public Guid GlobalPromptID { get; set; }

        [Required]
        [StringLength(200)]
        public string PromptName { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(5000)]
        public string MasterPromptTemplate { get; set; } = string.Empty; // Template chung cho AI

        [StringLength(2000)]
        public string? ScenarioGuidelines { get; set; } // Hướng dẫn tạo tình huống

        [StringLength(1000)]
        public string? RoleplayInstructions { get; set; } // Hướng dẫn về roleplay

        [StringLength(1000)]
        public string? EvaluationCriteria { get; set; } // Tiêu chí đánh giá

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; } = false; // Prompt mặc định được sử dụng

        public int UsageCount { get; set; } = 0; // Số lần được sử dụng
        public Guid LanguageID { get; set; }
        public virtual Language Language { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<ConversationSession>? ConversationSessions { get; set; } = new List<ConversationSession>();
    }
}

