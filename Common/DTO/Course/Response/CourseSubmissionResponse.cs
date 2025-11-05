namespace Common.DTO.Course.Response
{
    public class Submitter
    {
        public Guid TeacherId { get; set; }
        public string? Name { get; set; }
        public string? Avatar { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
    public class Reviewer
    {
        public Guid ManagerId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }
    public class CourseSubmissionResponse
    {
        public Guid SubmissionId { get; set; }
        public CourseResponse? Course { get; set; }
        public Submitter? Submitter { get; set; }
        public Reviewer? Reviewer { get; set; }
        public string? SubmissionStatus { get; set; }
        public string? Feedback { get; set; }
        public string? SubmittedAt { get; set; }
        public string? ReviewedAt { get; set; }
    }
}
