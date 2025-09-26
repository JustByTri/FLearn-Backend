using DAL.Type;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Course.Request
{
    public class CourseRequest
    {
        [Required]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;
        [Required]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; } = string.Empty;
        [Required]
        public required IFormFile Image { get; set; }
        [Required(ErrorMessage = "TemplateId is required.")]
        public Guid TemplateId { get; set; }
        [Required(ErrorMessage = "At least one topic is required.")]
        [MinLength(1, ErrorMessage = "Must provide at least one topic.")]
        public List<Guid> TopicIds { get; set; } = new();
        [Required]
        [Range(0, 100000, ErrorMessage = "Price must be between 0 and 100,000.")]
        public decimal Price { get; set; }
        [Range(0, 100000, ErrorMessage = "Discount price must be between 0 and 100,000.")]
        public decimal? DiscountPrice { get; set; }
        [Required(ErrorMessage = "Course type is required.")]
        public CourseType CourseType { get; set; }
        [Required(ErrorMessage = "LanguageID is required.")]
        public Guid LanguageID { get; set; }
        [Required(ErrorMessage = "GoalId is required.")]
        public int GoalId { get; set; }
        public LevelType? CourseLevel { get; set; }
        public SkillType? CourseSkill { get; set; }
    }
}
