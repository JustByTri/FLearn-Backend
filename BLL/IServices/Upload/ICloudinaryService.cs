using Common.DTO.Upload;
using Microsoft.AspNetCore.Http;

namespace BLL.IServices.Upload
{
    public interface ICloudinaryService
    {
        Task<string> UploadFileAsync(IFormFile file, string folder);
        Task<bool> DeleteFileAsync(string publicId);
        Task<string> UploadCredentialAsync(IFormFile file, Guid userId, string credentialName);


        Task<UploadResultDto> UploadImageAsync(IFormFile file, string folder = "general");
        Task<UploadResultDto> UploadDocumentAsync(IFormFile file, string folder = "documents");
        Task<List<UploadResultDto>> UploadMultipleFilesAsync(IList<IFormFile> files, string folder = "general");
        Task<UploadResultDto> UploadVideoAsync(IFormFile file, string folder = "videos");
    }
}
