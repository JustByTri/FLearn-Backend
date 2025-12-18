using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Statistics.Request
{
    public class ReviewDetailRequest
    {
        [Required]
        public Guid CourseId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int? Rating { get; set; }
    }
}
