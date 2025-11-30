namespace Common.DTO.AppReview.Response
{
    public class AppReviewResponse
    {
        public Guid ReviewId { get; set; }
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserAvatar { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }
}
