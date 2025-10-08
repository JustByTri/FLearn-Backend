namespace Common.DTO.Certificate.Response
{
    public class CertificateResponse
    {
        public Guid CertificateId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
