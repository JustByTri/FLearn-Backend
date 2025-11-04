namespace Common.DTO.CourseTemplate.Response
{
    public class CourseTemplateResponse
    {
        public Guid TemplateId { get; set; }
        public string? Program { get; set; }
        public string? Level { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int UnitCount { get; set; } = 0;
        public int LessonsPerUnit { get; set; } = 0;
        public int ExercisesPerLesson { get; set; } = 0;
        public string? ScoringCriteriaJson { get; set; }
        public string? Version { get; set; }
        public string? CreatedAt { get; set; }
        public string? ModifiedAt { get; set; }
    }
}
