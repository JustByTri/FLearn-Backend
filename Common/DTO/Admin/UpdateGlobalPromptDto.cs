using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Admin
{
    public class UpdateGlobalPromptDto
    {
        [Required(ErrorMessage = "Tên prompt là bắt buộc")]
        [StringLength(200)]
        public string PromptName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Master prompt template là bắt buộc")]
        [StringLength(5000)]
        public string MasterPromptTemplate { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? ScenarioGuidelines { get; set; }

        [StringLength(1000)]
        public string? RoleplayInstructions { get; set; }

        [StringLength(1000)]
        public string? EvaluationCriteria { get; set; }

        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
    }
}
