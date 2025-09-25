namespace Common.DTO.CourseTemplate.Response
{
    public class CourseTemplateResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool RequireGoal { get; set; }
        public bool RequireLevel { get; set; }
        public bool RequireSkillFocus { get; set; }
        public bool RequireTopic { get; set; }
        public bool RequireLang { get; set; }
        public int MinUnits { get; set; }
        public int MinLessonsPerUnit { get; set; }
        public int MinExercisesPerLesson { get; set; }
    }
}
