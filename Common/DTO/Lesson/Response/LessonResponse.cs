namespace Common.DTO.Lesson.Response
{
    public class LessonResponse
    {
        public Guid LessonID { get; set; }
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public int Position { get; set; }
        public string? SkillFocus { get; set; }
        public string? Description { get; set; }
        public string? VideoUrl { get; set; }
        public string? DocumentUrl { get; set; }
        public Guid CourseUnitID { get; set; }
        public string? UnitTitle { get; set; }
        public Guid CourseID { get; set; }
        public string? CourseTitle { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
