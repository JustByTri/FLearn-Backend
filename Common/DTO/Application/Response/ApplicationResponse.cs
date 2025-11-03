namespace Common.DTO.Application.Response
{
    public class ApplicationResponse
    {
        public Guid ApplicationID { get; set; }
        public string? Language { get; set; }
        public string? FullName { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? TeachingExperience { get; set; }
        public string? ProficiencyCode { get; set; }
        public string? MeetingUrl { get; set; }
        public string? RejectionReason { get; set; }
        public string? Status { get; set; }
        public string? SubmittedAt { get; set; }
        public string? ReviewedAt { get; set; }
        public IEnumerable<ApplicationCertTypeResponse> Certificates { get; set; } = new List<ApplicationCertTypeResponse>();
        public UserResponse? Submitter { get; set; }
        public UserResponse? Reviewer { get; set; }
    }
}
