using Common.DTO.Language;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Application.Request
{
    public class ApplicationRequest
    {
        [Required(ErrorMessage = "Language code is required.")]
        [AllowedLang]
        public string LangCode { get; set; } = string.Empty;
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Birth date is required.")]
        public DateTime BirthDate { get; set; }
        [Required(ErrorMessage = "Bio is required.")]
        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
        public string Bio { get; set; } = string.Empty;
        [Required(ErrorMessage = "Avatar image file is required.")]
        public IFormFile Avatar { get; set; } = null!;
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters.")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string PhoneNumber { get; set; } = string.Empty;
        [Required(ErrorMessage = "Teaching experience is required.")]
        [StringLength(500, ErrorMessage = "Teaching experience cannot exceed 500 characters.")]
        public string TeachingExperience { get; set; } = string.Empty;
        [Required(ErrorMessage = "Proficiency code is required.")]
        [StringLength(50, ErrorMessage = "Proficiency code cannot exceed 50 characters.")]
        public string ProficiencyCode { get; set; } = string.Empty;
        [Required(ErrorMessage = "Meeting URL is required.")]
        [Url(ErrorMessage = "Meeting URL must be a valid URL.")]
        [StringLength(500, ErrorMessage = "Meeting URL cannot exceed 500 characters.")]
        public string MeetingUrl { get; set; } = string.Empty;
        [Required(ErrorMessage = "At least one certificate image is required.")]
        public IFormFile[] CertificateImages { get; set; } = Array.Empty<IFormFile>();
        [Required(ErrorMessage = "CertificateTypeIds must match CertificateImages count.")]
        public string[] CertificateTypeIds { get; set; } = Array.Empty<string>();
    }
}
