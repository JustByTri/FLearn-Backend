using Common.DTO.Topic.Response;

namespace Common.DTO.Course.Response
{
    public class TeacherInfo
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
    public class UserInfo
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
    public class TemplateInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    public class LanguageInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
    }
    public class GoalInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
    public class CourseResponse
    {
        public Guid CourseID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public TemplateInfo? TemplateInfo { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string CourseType { get; set; } = string.Empty;
        public TeacherInfo? TeacherInfo { get; set; }
        public LanguageInfo? LanguageInfo { get; set; }
        public GoalInfo? GoalInfo { get; set; }
        public string CourseLevel { get; set; } = string.Empty;
        public string CourseSkill { get; set; } = string.Empty;
        public string? PublishedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string ModifiedAt { get; set; } = string.Empty;
        public int NumLessons { get; set; }
        public UserInfo? ApprovedBy { get; set; }
        public string? ApprovedAt { get; set; }
        public List<TopicResponse> Topics { get; set; } = new();
    }
}
