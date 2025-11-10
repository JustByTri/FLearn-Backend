namespace Common.DTO.Enrollment.Response
{
    public class EnrolledCourseOverviewResponse
    {
        public Guid EnrollmentId { get; set; }
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; } = null!;
        public string CourseImage { get; set; } = null!;
        public string Language { get; set; } = null!;
        public string Level { get; set; } = null!;
        public string TeacherName { get; set; } = null!;
        public string TeacherAvatar { get; set; } = null!;
        public double ProgressPercent { get; set; }
        public string Status { get; set; } = null!;
        public string? LastAccessedAt { get; set; }
        public string EnrolledAt { get; set; } = null!;
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public int TotalUnits { get; set; }
        public int CompletedUnits { get; set; }
        public string? CurrentUnit { get; set; }
        public string? CurrentLesson { get; set; }
        public string? NextLesson { get; set; }
        public bool IsExpired { get; set; }
        public string? AccessUntil { get; set; }
    }
}
