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
        public string PromptName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public string MasterPromptTemplate { get; set; }

        public string ScenarioGuidelines { get; set; }
        public string RoleplayInstructions { get; set; }
        public string EvaluationCriteria { get; set; }

        // ✅ NEW: Status field - Draft, Active,
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // Draft, Active, 

        public bool IsActive { get; set; } = false;
        public bool IsDefault { get; set; } = false;

    

        public int UsageCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

       
        public Guid? CreatedByAdminId { get; set; }
        public Guid? LastModifiedByAdminId { get; set; }
    }
}

