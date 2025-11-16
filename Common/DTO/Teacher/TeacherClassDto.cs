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
        public DateTime StartDateTime { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime EndDateTime { get; set; } = TimeHelper.GetVietnamTime();
        public int Capacity { get; set; }
        public decimal PricePerStudent { get; set; }
        public string? GoogleMeetLink { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CurrentEnrollments { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
