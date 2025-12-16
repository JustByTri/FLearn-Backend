using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Topic.Request
{
    public class TopicRequest
    {
        [Required(ErrorMessage = "Topic name is required.")]
        [StringLength(100, ErrorMessage = "Topic name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "Context prompt is required.")]
        [StringLength(2000, ErrorMessage = "Context prompt cannot exceed 2000 characters.")]
        public string ContextPrompt { get; set; } = string.Empty;
        public bool? Status { get; set; }
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;
        [Required(ErrorMessage = "Image is required.")]
        public required IFormFile Image { get; set; }
    }
}
