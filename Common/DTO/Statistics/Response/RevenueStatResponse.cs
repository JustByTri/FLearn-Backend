namespace Common.DTO.Statistics.Response
{
    public class RevenueStatResponse
    {
        public int Month { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TransactionCount { get; set; }
    }
    public class CourseRevenueYearlyResponse
    {
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; }
        public int Year { get; set; }
        public decimal TotalYearlyRevenue { get; set; }
        public List<RevenueStatResponse> MonthlyBreakdown { get; set; }
    }
}
