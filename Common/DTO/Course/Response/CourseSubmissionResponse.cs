namespace Common.DTO.Course.Response
{
    public class CourseSubmissionResponse
    {
        public Guid CourseSubmissionID { get; set; }
        public string SubmissionStatus { get; set; }
        public string? Feedback { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public CourseResponse Course { get; set; }
    }
}
