using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class ApplicationCertType
    {
        [Key]
        public Guid ApplicationCertTypeId { get; set; }
        public Guid ApplicationId { get; set; }
        [ForeignKey(nameof(ApplicationId))]
        public virtual TeacherApplication Application { get; set; }
        public Guid? CertificateTypeId { get; set; }
        [ForeignKey(nameof(CertificateTypeId))]
        public virtual CertificateType? CertificateType { get; set; }
        public string CertificateImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
