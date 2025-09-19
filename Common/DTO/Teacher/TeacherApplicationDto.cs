using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Teacher
{
    public class TeacherApplicationDto
    {
        public Guid TeacherApplicationID { get; set; }
        public Guid UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid LanguageID { get; set; } 
        public string LanguageName { get; set; }
        public string Motivation { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public DateTime? SubmitAt { get; set; }
        public DateTime? ReviewAt { get; set; }
        public Guid? ReviewedBy { get; set; }
        public string? ReviewerName { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; }
        public List<TeacherCredentialDto> Credentials { get; set; } = new List<TeacherCredentialDto>();
        public DateTime CreatedAt { get; set; }
    }

    public enum ApplicationStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        UnderReview = 3
    }
}
