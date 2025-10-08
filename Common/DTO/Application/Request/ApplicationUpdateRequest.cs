using Common.DTO.Language;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Application.Request
{
    public class ApplicationUpdateRequest
    {
        [Required(ErrorMessage = "Language code is required.")]
        [AllowedLang]
        public string LangCode { get; set; } = string.Empty;
        [StringLength(100)]
        public string? FullName { get; set; }
        public DateTime? BirthDate { get; set; }
        [StringLength(500)]
        public string? Bio { get; set; }
        public IFormFile? Avatar { get; set; }
        [EmailAddress]
        [StringLength(200)]
        public string? Email { get; set; }
        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        [StringLength(500)]
        public string? TeachingExperience { get; set; }
        public string? MeetingUrl { get; set; }
        public IFormFile[]? CertificateImages { get; set; }
        public string[]? CertificateTypeIds { get; set; }
    }
}
