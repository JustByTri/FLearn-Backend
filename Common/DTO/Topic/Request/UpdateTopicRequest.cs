using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Topic.Request
{
    public class UpdateTopicRequest
    {
        [StringLength(100, ErrorMessage = "Topic name cannot exceed 100 characters.")]
        public string? Name { get; set; }
        [StringLength(2000, ErrorMessage = "Context prompt cannot exceed 2000 characters.")]
        public string? ContextPrompt { get; set; }
        public bool? Status { get; set; }
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }
        public IFormFile? Image { get; set; }
    }
}
