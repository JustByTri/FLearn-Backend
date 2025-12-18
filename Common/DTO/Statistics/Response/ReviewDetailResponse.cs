namespace Common.DTO.Statistics.Response
{
    public class ReviewDetailItem
    {
        public Guid ReviewId { get; set; }
        public string LearnerName { get; set; }
        public string LearnerAvatar { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PagedReviewResponse
    {
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public List<ReviewDetailItem> Reviews { get; set; }
    }
}
