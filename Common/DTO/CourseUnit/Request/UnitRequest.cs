using System.ComponentModel.DataAnnotations;

namespace Common.DTO.CourseUnit.Request
{
    public class UnitRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot exceed {1} characters.")]
        public string Title { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed {1} characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "CourseID is required.")]
        public bool? IsPreview { get; set; } = false;
    }
}
