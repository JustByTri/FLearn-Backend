using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Admin
{
    public class LevelCreateDto
    {
        [Required]
        public Guid ProgramId { get; set; }

        [Required(ErrorMessage = "Tên cấp độ là bắt buộc")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; 

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

    }
}
