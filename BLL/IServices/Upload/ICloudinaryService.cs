using Common.DTO.Upload;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
