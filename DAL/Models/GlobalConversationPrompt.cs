using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class GlobalConversationPrompt
    {
        [Key]
        public Guid GlobalPromptID { get; set; }
        [Required]
        [StringLength(200)]
        public string? PromptName { get; set; }
        [StringLength(500)]
        public string? Description { get; set; }
        [Required]
        public string? MasterPromptTemplate { get; set; }
        public string? ScenarioGuidelines { get; set; }
        public string? RoleplayInstructions { get; set; }
        public string? EvaluationCriteria { get; set; }
        [StringLength(50)]
        public string Status { get; set; } = "Draft";
        public bool IsActive { get; set; } = false;
        public bool IsDefault { get; set; } = false;
        public int UsageCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public Guid? CreatedByAdminId { get; set; }
        [ForeignKey(nameof(CreatedByAdminId))]
        public virtual User? CreatedByAdmin { get; set; }
        public Guid? LastModifiedByAdminId { get; set; }
        [ForeignKey(nameof(LastModifiedByAdminId))]
        public virtual User? LastModifiedByAdmin { get; set; }
        public virtual ICollection<ConversationSession> ConversationSessions { get; set; } = new List<ConversationSession>();
    }
}

