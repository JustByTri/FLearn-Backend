namespace Common.DTO.Statistics.Response
{
    public class EnrollmentStatResponse
    {
        public int Month { get; set; }
        public int NewEnrollments { get; set; }
    }
    public class CourseEnrollmentYearlyResponse
    {
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; }
        public int Year { get; set; }
        public int TotalYearlyEnrollments { get; set; }
        public List<EnrollmentStatResponse> MonthlyBreakdown { get; set; }
    }
}
