using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Admin
{
    public class CreateGlobalPromptDto
    {
        [Required(ErrorMessage = "Tên prompt là bắt buộc")]
        
        public string PromptName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
      
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Master prompt template là bắt buộc")]

        public string MasterPromptTemplate { get; set; } = string.Empty;


        public string? ScenarioGuidelines { get; set; }

 
        public string? RoleplayInstructions { get; set; }

  
        public string? EvaluationCriteria { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
    }
}
