using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Admin
{
    public class GlobalConversationPromptDto
    {
        public Guid GlobalPromptID { get; set; }
        public string PromptName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MasterPromptTemplate { get; set; } = string.Empty;
        public string? ScenarioGuidelines { get; set; }
        public string? RoleplayInstructions { get; set; }
        public string? EvaluationCriteria { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public int UsageCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
