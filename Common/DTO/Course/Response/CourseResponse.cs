using Common.DTO.Goal.Response;
using Common.DTO.Topic.Response;

namespace Common.DTO.Course.Response
{
    public class TeacherInfo
    {
        public Guid TeacherId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
    public class StaffInfo
    {
        public Guid StaffId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
    public class TemplateInfo
    {
        public Guid TemplateId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    public class LanguageInfo
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
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
        public List<GoalResponse> Goals { get; set; } = new();
        public string CourseLevel { get; set; } = string.Empty;
        public string? PublishedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string ModifiedAt { get; set; } = string.Empty;
        public int NumLessons { get; set; }
        public StaffInfo? ApprovedBy { get; set; }
        public string? ApprovedAt { get; set; }
        public List<TopicResponse> Topics { get; set; } = new();
    }
}
