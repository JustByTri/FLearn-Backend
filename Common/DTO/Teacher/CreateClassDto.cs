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

       
        [Required(ErrorMessage = "Ngày diễn ra là bắt buộc")]
        public DateTime ClassDate { get; set; }  // Chỉ ngày: 2025-11-05

        [Required(ErrorMessage = "Giờ bắt đầu là bắt buộc")]
        public TimeSpan StartTime { get; set; }  // Chỉ giờ: 14:30:00

        [Required(ErrorMessage = "Thời lượng là bắt buộc")]
        [AllowedDuration]  // Custom validation
        public int DurationMinutes { get; set; }  // 45, 60, 90, hoặc 120

        [Required(ErrorMessage = "Giá học phí là bắt buộc")]
        [Range(0.01, 10000000, ErrorMessage = "Giá học phí phải lớn hơn 0")]
        public decimal PricePerStudent { get; set; }

        [StringLength(500, ErrorMessage = "Link Google Meet không được vượt quá 500 ký tự")]
        public string GoogleMeetLink { get; set; }
    }

 
    public class AllowedDurationAttribute : ValidationAttribute
    {
        private readonly int[] _allowedDurations = { 45, 60, 90, 120 };

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is int duration)
            {
                if (_allowedDurations.Contains(duration))
                {
                    return ValidationResult.Success;
                }
                return new ValidationResult("Thời lượng chỉ được phép là 45, 60, 90 hoặc 120 phút");
            }
            return new ValidationResult("Giá trị không hợp lệ");
        }
    }
}
