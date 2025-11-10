namespace Common.DTO.Enrollment.Response
{
    public class EnrolledCourseDetailResponse
    {
        public Guid EnrollmentId { get; set; }
        public CourseDetailDto Course { get; set; } = null!;
        public ProgressDetailDto Progress { get; set; } = null!;
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
    }
    public class CourseDetailDto
    {
        public Guid CourseId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Image { get; set; } = null!;
        public string Language { get; set; } = null!;
        public string Level { get; set; } = null!;
        public string Duration { get; set; } = null!;
        public int TotalUnits { get; set; }
        public int TotalLessons { get; set; }
        public int TotalExercises { get; set; }
        public TeacherInfoDto Teacher { get; set; } = null!;
        public string Objective { get; set; } = null!;
    }
    public class ProgressDetailDto
    {
        public double OverallPercent { get; set; }
        public string TotalTimeSpent { get; set; } = null!;
        public string? LastAccessed { get; set; }
        public int CompletedUnits { get; set; }
        public int CompletedLessons { get; set; }
        public CurrentUnitDto? CurrentUnit { get; set; }
        public CurrentLessonDto? CurrentLesson { get; set; }
        public UpcomingLessonDto? UpcomingLesson { get; set; }
    }
    public class CurrentUnitDto
    {
        public Guid UnitId { get; set; }
        public string Title { get; set; } = null!;
        public double ProgressPercent { get; set; }
    }
    public class CurrentLessonDto
    {
        public Guid LessonId { get; set; }
        public string Title { get; set; } = null!;
        public double ProgressPercent { get; set; }
    }
    public class UpcomingLessonDto
    {
        public Guid LessonId { get; set; }
        public string Title { get; set; } = null!;
    }
    public class RecentActivityDto
    {
        public string Type { get; set; } = null!; // LessonCompleted, ExerciseSubmitted, etc.
        public string Title { get; set; } = null!;
        public string Time { get; set; } = null!; // "2 hours ago", "1 day ago"
    }
    public class TeacherInfoDto
    {
        public Guid TeacherId { get; set; }
        public string Name { get; set; } = null!;
        public string Avatar { get; set; } = null!;
        public double Rating { get; set; }
        public int TotalStudents { get; set; }
    }
}
