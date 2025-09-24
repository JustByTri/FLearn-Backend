using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Topic.Request
{
    public class TopicRequest
    {
        [Required(ErrorMessage = "Topic name is required.")]
        [StringLength(100, ErrorMessage = "Topic name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;
    }
}
