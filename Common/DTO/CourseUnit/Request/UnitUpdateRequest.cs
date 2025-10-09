using System.ComponentModel.DataAnnotations;

namespace Common.DTO.CourseUnit.Request
{
    public class UnitUpdateRequest
    {
        [StringLength(200)]
        public string? Title { get; set; }
        [StringLength(500)]
        public string? Description { get; set; }
        public bool? IsPreview { get; set; }
    }
}
