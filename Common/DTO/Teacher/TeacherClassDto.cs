using DAL.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Teacher
{
    public class TeacherClassDto
    {
        public Guid ClassID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid LanguageID { get; set; }
        public string? LanguageName { get; set; }
        public string Program {get; set; } = string.Empty;

        /// <summary>
        /// Tên giáo viên (ưu tiên UserName, fallback FullName)
        /// </summary>
        public string TeacherName { get; set; } = string.Empty;

        public DateTime StartDateTime { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime EndDateTime { get; set; } = TimeHelper.GetVietnamTime();

        /// <summary>
        /// Số học sinh tối thiểu để lớp được diễn ra
        /// </summary>
        public int MinStudents { get; set; }

        /// <summary>
        /// Sức chứa tối đa của lớp
        /// </summary>
        public int Capacity { get; set; }

        public decimal PricePerStudent { get; set; }
        public string? GoogleMeetLink { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CurrentEnrollments { get; set; }

        /// <summary>
        /// Số slot còn trống
        /// </summary>
        public int AvailableSlots => Capacity - CurrentEnrollments;

        /// <summary>
        /// Còn thiếu bao nhiêu học sinh để đủ tối thiểu
        /// </summary>
        public int StudentsNeeded => Math.Max(0, MinStudents - CurrentEnrollments);

        /// <summary>
        /// Lớp đã đủ học sinh tối thiểu chưa
        /// </summary>
        public bool HasMinimumStudents => CurrentEnrollments >= MinStudents;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
