using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Learner
{
    public class EnrolledClassDto
    {
        public Guid EnrollmentID { get; set; }
        public Guid ClassID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid LanguageID { get; set; }
        public string LanguageName { get; set; }
        public string TeacherName { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentTransactionId { get; set; }
        public string EnrollmentStatus { get; set; }
        public string ClassStatus { get; set; }
        public DateTime EnrolledAt { get; set; }
        public int TotalEnrollments { get; set; }
        public int Capacity { get; set; }
        public string GoogleMeetLink { get; set; }
        public bool CanJoinClass { get; set; }
        public bool IsClassStarted { get; set; }
        public bool IsClassFinished { get; set; }
    }
}
