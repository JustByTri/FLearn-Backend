using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Teacher
{
    public class CreateClassDto
    {
        [Required(ErrorMessage = "Tiêu đề lớp học là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Title { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Ngôn ngữ là bắt buộc")]
        public Guid LanguageID { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime StartDateTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public DateTime EndDateTime { get; set; }

        [Required(ErrorMessage = "Số học sinh tối thiểu là bắt buộc")]
        [Range(1, 50, ErrorMessage = "Số học sinh tối thiểu phải từ 1-50")]
        public int MinStudents { get; set; }

        [Required(ErrorMessage = "Sức chứa lớp học là bắt buộc")]
        [Range(1, 100, ErrorMessage = "Sức chứa phải từ 1-100")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Giá học phí là bắt buộc")]
        [Range(0.01, 10000000, ErrorMessage = "Giá học phí phải lớn hơn 0")]
        public decimal PricePerStudent { get; set; }

        [StringLength(500, ErrorMessage = "Link Google Meet không được vượt quá 500 ký tự")]
        public string GoogleMeetLink { get; set; }
    }
}
