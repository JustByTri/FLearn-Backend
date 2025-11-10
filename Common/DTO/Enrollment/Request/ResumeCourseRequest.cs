namespace Common.DTO.Enrollment.Request
{
    public class ResumeCourseRequest
    {
        public Guid LessonId { get; set; }
        public Guid UnitId { get; set; }
    }
}
