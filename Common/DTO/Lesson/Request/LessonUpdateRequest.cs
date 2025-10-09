using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Lesson.Request
{
    public class LessonUpdateRequest
    {
        [StringLength(200, ErrorMessage = "Title cannot exceed {1} characters.")]
        public string? Title { get; set; }
        [StringLength(500, ErrorMessage = "Description cannot exceed {1} characters.")]
        public string? Description { get; set; }
        [StringLength(int.MaxValue, ErrorMessage = "Content length is too large.")]
        public string? Content { get; set; }
        /// <summary>
        /// Optional video file (mp4, mov, etc.)
        /// </summary>
        public IFormFile? VideoFile { get; set; }
        /// <summary>
        /// Optional document file (pdf, docx, etc.)
        /// </summary>
        public IFormFile? DocumentFile { get; set; }
    }
}
