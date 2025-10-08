using Microsoft.AspNetCore.Http;

namespace Common.DTO.Application.Request
{
    public class ApplicationCertificateRequest
    {
        public IFormFile? CertificateImage { get; set; }
        public Guid? CertificateId { get; set; }
    }
}
