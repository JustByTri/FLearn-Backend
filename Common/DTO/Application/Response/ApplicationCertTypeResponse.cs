using Common.DTO.Certificate.Response;

namespace Common.DTO.Application.Response
{
    public class ApplicationCertTypeResponse
    {
        public Guid ApplicationCertTypeId { get; set; }
        public Guid? CertificateTypeId { get; set; }
        public string CertificateImageUrl { get; set; } = string.Empty;
        public CertificateResponse? CertificateType { get; set; }
    }
}
