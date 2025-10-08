using Common.DTO.Language.Response;

namespace Common.DTO.Application.Response
{
    public class ApplicationResponse
    {
        public Guid ApplicationID { get; set; }
        public Guid UserID { get; set; }
        public Guid LanguageID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string Bio { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string TeachingExperience { get; set; } = string.Empty;
        public string MeetingUrl { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public string Status { get; set; }
        public Guid? ReviewedBy { get; set; }
        public string? ReviewedByName { get; set; }  // Optional: for convenience
        public DateTime SubmittedAt { get; set; }
        public DateTime ReviewedAt { get; set; }
        public LanguageResponse? Language { get; set; }
        public UserResponse? User { get; set; }
        public IEnumerable<ApplicationCertTypeResponse> Certificates { get; set; } = new List<ApplicationCertTypeResponse>();
    }
}
