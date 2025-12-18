using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Teacher
{
    public class UpdateClassDto
    {
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string? Title { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string? Description { get; set; }

        public DateTime? StartDateTime { get; set; }

        public DateTime? EndDateTime { get; set; }

        [Range(1, 50, ErrorMessage = "Số học sinh tối thiểu phải từ 1-50")]
        public int? MinStudents { get; set; }

        [Range(1, 100, ErrorMessage = "Sức chứa phải từ 1-100")]
        public int? Capacity { get; set; }

        [Range(0.01, 10000000, ErrorMessage = "Giá học phí phải lớn hơn 0")]
        public decimal? PricePerStudent { get; set; }

        [StringLength(500, ErrorMessage = "Link Google Meet không được vượt quá 500 ký tự")]
        public string? GoogleMeetLink { get; set; }

        [Range(1, 1000, ErrorMessage = "Thời lượng phải lớn hơn 0")]
        public int? DurationMinutes { get; set; }
    }
}
