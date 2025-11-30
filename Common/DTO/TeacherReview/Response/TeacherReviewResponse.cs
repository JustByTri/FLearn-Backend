namespace Common.DTO.TeacherReview.Response
{
    public class TeacherReviewResponse
    {
        public Guid TeacherReviewId { get; set; }
        public Guid LearnerId { get; set; }
        public string LearnerName { get; set; } = string.Empty;
        public string LearnerAvatar { get; set; } = string.Empty;
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string ModifiedDate { get; set; } = string.Empty;
    }
}
