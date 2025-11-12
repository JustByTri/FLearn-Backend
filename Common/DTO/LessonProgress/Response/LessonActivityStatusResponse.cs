namespace Common.DTO.LessonProgress.Response
{
    public class LessonActivityStatusResponse
    {
        public Guid LessonId { get; set; }
        public string LessonTitle { get; set; } = null!;
        public bool IsContentViewed { get; set; }
        public bool IsVideoWatched { get; set; }
        public bool IsDocumentRead { get; set; }
        public bool IsPracticeCompleted { get; set; }
        public ActivityDetail Content { get; set; } = null!;
        public ActivityDetail Video { get; set; } = null!;
        public ActivityDetail Document { get; set; } = null!;
        public double CalculatedProgress { get; set; }
        public string ProgressBreakdown { get; set; } = null!;
        public bool MeetsCompletionRequirements { get; set; }
        public List<string> MissingRequirements { get; set; } = new();
    }
}
