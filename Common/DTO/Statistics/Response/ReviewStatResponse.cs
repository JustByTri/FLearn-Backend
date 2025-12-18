namespace Common.DTO.Statistics.Response
{
    public class StarDistribution
    {
        public int Star { get; set; } // 1, 2, 3, 4, 5
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
    public class CourseReviewStatResponse
    {
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<StarDistribution> StarDistribution { get; set; }
    }
}
