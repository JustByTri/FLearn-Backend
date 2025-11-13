using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Common.Request
{
    public class ToggleVisibilityRequest
    {
        [Required]
        public bool IsHidden { get; set; }
    }
}
