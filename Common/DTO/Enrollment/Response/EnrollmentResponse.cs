namespace Common.DTO.Enrollment.Response
{
    public class EnrollmentResponse
    {
        public Guid EnrollmentID { get; set; }
        public Guid CourseId { get; set; }
        public Guid LearnerId { get; set; }
        public string EnrolledAt { get; set; } = string.Empty;
        public CourseBasicInfo Course { get; set; }
        public string? Status { get; set; }
        public double ProgressPercent { get; set; }
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
    }
    public class CourseBasicInfo
    {
        public Guid CourseID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public string CourseType { get; set; } = string.Empty;
        public string CourseLevel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public TeacherInfo TeacherInfo { get; set; }
    }
    public class TeacherInfo
    {
        public Guid TeacherId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
