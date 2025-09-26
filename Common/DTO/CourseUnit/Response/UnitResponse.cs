namespace Common.DTO.CourseUnit.Response
{
    public class UnitResponse
    {
        public Guid CourseUnitID { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int Position { get; set; }
        public Guid CourseID { get; set; }
        public string? CourseTitle { get; set; }
        public int TotalLessons { get; set; }
        public bool? IsPreview { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
