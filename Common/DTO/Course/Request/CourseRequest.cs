using DAL.Type;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Course.Request
{
    public class CourseRequest
    {
        [Required]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; }
        [Required]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; }
        [Required(ErrorMessage = "Image is required.")]
        public IFormFile Image { get; set; }
        [Required(ErrorMessage = "TemplateId is required.")]
        public Guid TemplateId { get; set; }
        [Required(ErrorMessage = "At least one topic is required.")]
        [MinLength(1, ErrorMessage = "Must provide at least one topic.")]
        public string TopicIds { get; set; }
        [Required]
        [Range(0, 5_000_000, ErrorMessage = "Price must be between 0 and 5,000,000VND.")]
        public decimal Price { get; set; }
        [Range(0, 5_000_000, ErrorMessage = "Discount price must be between 0 and 5,000,000VND.")]
        public decimal? DiscountPrice { get; set; }
        [Required(ErrorMessage = "Course type is required.")]
        public CourseType CourseType { get; set; }
        [Required(ErrorMessage = "GoalId is required.")]
        public int GoalId { get; set; }
        [Required(ErrorMessage = "Level is required.")]
        public LevelType Level { get; set; }
    }
}
