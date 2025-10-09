using DAL.Type;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Course.Request
{
    public class UpdateCourseRequest
    {
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string? Title { get; set; }
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }
        public IFormFile? Image { get; set; }
        public Guid? TemplateId { get; set; }
        public Guid[]? TopicIds { get; set; }
        [Required]
        [Range(0, 5_000_000, ErrorMessage = "Price must be between 0 and 5,000,000VND.")]
        public decimal Price { get; set; }
        [Range(0, 5_000_000, ErrorMessage = "Discount price must be between 0 and 5,000,000VND.")]
        public decimal? DiscountPrice { get; set; }
        public CourseType? Type { get; set; }
        public int? GoalId { get; set; }
        public LevelType? Level { get; set; }
    }
}
