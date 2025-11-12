namespace Common.DTO.LessonProgress.Response
{
    public class LessonProgressSummaryResponse
    {
        public Guid LessonId { get; set; }
        public string Title { get; set; } = null!;
        public int Order { get; set; }
        public double ProgressPercent { get; set; }
        public string Status { get; set; } = null!;
        public bool IsContentViewed { get; set; }
        public bool IsVideoWatched { get; set; }
        public bool IsDocumentRead { get; set; }
        public bool IsPracticeCompleted { get; set; }
        public int TotalExercises { get; set; }
        public int CompletedExercises { get; set; }
    }
}
