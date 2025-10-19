using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Learner
{
    public class AvailableClassDto
    {
        public Guid ClassID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid LanguageID { get; set; }
        public string LanguageName { get; set; }
        public string TeacherName { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int MinStudents { get; set; }
        public int Capacity { get; set; }
        public decimal PricePerStudent { get; set; }
        public string Status { get; set; }
        public int CurrentEnrollments { get; set; }
        public int AvailableSlots { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsEnrollmentOpen { get; set; }
    }
}
