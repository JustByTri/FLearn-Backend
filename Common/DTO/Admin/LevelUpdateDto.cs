using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Admin
{
    public class LevelUpdateDto
    {
        [Required(ErrorMessage = "Tên cấp độ là bắt buộc")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, 100, ErrorMessage = "Thứ tự phải từ 1 đến 100")]
        public int OrderIndex { get; set; }

        [Required]
        public bool Status { get; set; } = true;
    }
}
