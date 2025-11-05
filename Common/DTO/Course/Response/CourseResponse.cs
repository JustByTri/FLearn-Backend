using Common.DTO.CourseUnit.Response;
using Common.DTO.Topic.Response;

namespace Common.DTO.Course.Response
{
    public class CourseResponse
    {
        public Guid CourseId { get; set; }
        public Guid TemplateId { get; set; }
        public string? Language { get; set; }
        public Program? Program { get; set; }
        public Teacher? Teacher { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? LearningOutcome { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? CourseType { get; set; }
        public string? GradingType { get; set; }
        public int LearnerCount { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int NumLessons { get; set; }
        public int NumUnits { get; set; }
        public int DurationDays { get; set; }
        public int EstimatedHours { get; set; }
        public string? CourseStatus { get; set; } = string.Empty;
        public string? PublishedAt { get; set; }
        public string? CreatedAt { get; set; }
        public string? ModifiedAt { get; set; }
        public ApprovedBy? ApprovedBy { get; set; }
        public string? ApprovedAt { get; set; }
        public List<TopicResponse> Topics { get; set; } = new List<TopicResponse>();
        public List<UnitResponse> Units { get; set; } = new List<UnitResponse>();
    }
    public class Program
    {
        public Guid ProgramId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Level? Level { get; set; }
    }
    public class Level
    {
        public Guid LevelId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
    public class Teacher
    {
        public Guid TeacherId { get; set; }
        public string? Name { get; set; }
        public string? Avatar { get; set; }
        public string? Email { get; set; }
    }
    public class ApprovedBy
    {
        public Guid ManagerId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }
}
