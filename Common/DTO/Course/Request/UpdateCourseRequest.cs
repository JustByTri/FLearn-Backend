using DAL.Type;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Course.Request
{
    public class UpdateCourseRequest
    {
        public Guid? TemplateId { get; set; }
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string? Title { get; set; }
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }
        [StringLength(1000)]
        public string? LearningOutcome { get; set; }
        public IFormFile? Image { get; set; }
        [Range(0, 5_000_000, ErrorMessage = "Price must be between 0VND and 5,000,000VND.")]
        public decimal? Price { get; set; }
        public CourseType? CourseType { get; set; }
        public GradingType? GradingType { get; set; }
        public int? DurationDays { get; set; }
        public string? TopicIds { get; set; }
    }
}
