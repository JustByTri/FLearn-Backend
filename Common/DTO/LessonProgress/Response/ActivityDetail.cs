namespace Common.DTO.LessonProgress.Response
{
    public class ActivityDetail
    {
        public bool IsAvailable { get; set; }
        public bool IsCompleted { get; set; }
        public string? CompletedAt { get; set; }
        public string? ResourceUrl { get; set; }
        public string? ResourceTitle { get; set; }
    }
}
