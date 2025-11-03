namespace Common.DTO.Application.Uploads
{
    public class ApplicationUploadInfo
    {
        public Guid ApplicationId { get; set; }
        public string AvatarPath { get; set; } = string.Empty;
        public List<CertificateUploadInfo> Certificates { get; set; } = new();
    }

    public class CertificateUploadInfo
    {
        public string CertificateTypeId { get; set; } = string.Empty;
        public string CertificateImagePath { get; set; } = string.Empty;
    }
}
