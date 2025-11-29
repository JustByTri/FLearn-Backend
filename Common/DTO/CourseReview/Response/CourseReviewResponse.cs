namespace Common.DTO.CourseReview.Response
{
    public class CourseReviewResponse
    {
        public Guid CourseReviewId { get; set; }
        public Guid LearnerId { get; set; }
        public string? LearnerName { get; set; }
        public string? LearnerAvatar { get; set; }
        public Guid CourseId { get; set; }
        public string? CourseTitle { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string? CreatedAt { get; set; }
        public string? ModifiedDate { get; set; }
    }
}
