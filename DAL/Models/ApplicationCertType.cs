using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class ApplicationCertType
    {
        [Key]
        public Guid ApplicationCertTypeId { get; set; }
        [Required]
        public Guid ApplicationId { get; set; }
        [ForeignKey(nameof(ApplicationId))]
        public virtual TeacherApplication TeacherApplication { get; set; } = null!;
        public Guid? CertificateTypeId { get; set; }
        [ForeignKey(nameof(CertificateTypeId))]
        public virtual CertificateType CertificateType { get; set; } = null!;
        public string CertificateImageUrl { get; set; } = string.Empty;
        public string CertificateImagePublicId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
    }
}
