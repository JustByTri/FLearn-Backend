using DAL.Type;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Course.Request
{
    public class CourseRequest
    {
        [Required(ErrorMessage = "LevelId is required.")]
        public Guid LevelId { get; set; }
        public Guid? TemplateId { get; set; }
        [Required]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string? Title { get; set; }
        [Required]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }
        [Required(ErrorMessage = "LearningOutcome is required.")]
        [StringLength(1000)]
        public string? LearningOutcome { get; set; }
        [Required(ErrorMessage = "Image is required.")]
        public IFormFile? Image { get; set; }
        [Required(ErrorMessage = "At least one topic is required.")]
        [MinLength(1, ErrorMessage = "Must provide at least one topic.")]
        public string? TopicIds { get; set; }
        [Required(ErrorMessage = "Price is required.")]
        [Range(0, 5_000_000, ErrorMessage = "Price must be between 0VND and 5,000,000VND.")]
        public decimal Price { get; set; }
        [Required(ErrorMessage = "Course type is required.")]
        public CourseType CourseType { get; set; }
        [Required(ErrorMessage = "Grading type is required.")]
        public GradingType GradingType { get; set; }
        public int DurationDays { get; set; } = 30;
    }
}
