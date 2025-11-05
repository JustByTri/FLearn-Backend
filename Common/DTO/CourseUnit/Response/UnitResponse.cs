using Common.DTO.Lesson.Response;

namespace Common.DTO.CourseUnit.Response
{
    public class UnitResponse
    {
        public Guid CourseUnitID { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int Position { get; set; }
        public Guid CourseID { get; set; }
        public string? CourseTitle { get; set; }
        public int TotalLessons { get; set; }
        public bool? IsPreview { get; set; }
        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
        public List<LessonResponse> Lessons { get; set; } = new List<LessonResponse>();
    }
}
