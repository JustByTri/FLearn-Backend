using BLL.IServices.Upload;
using BLL.Settings;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Common.DTO.Upload;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BLL.Services.Upload
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> config)
        {
            var account = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            var result = await UploadFileInternalAsync(file, folder);
            return result.Url;
        }

        public async Task<UploadResultDto> UploadImageAsync(IFormFile file, string folder = "general")
        {
            // Validate file
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ");

            // Check file size (max 5MB for images)
            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("File ảnh không được vượt quá 5MB");

            // Check file extension for images
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("Chỉ hỗ trợ file ảnh: JPG, JPEG, PNG, GIF, WEBP, BMP");

            return await UploadFileInternalAsync(file, folder);
        }

        public async Task<UploadResultDto> UploadDocumentAsync(IFormFile file, string folder = "documents")
        {
            // Validate file
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ");

            // Check file size (max 10MB for documents)
            if (file.Length > 10 * 1024 * 1024)
                throw new ArgumentException("File tài liệu không được vượt quá 10MB");

            // Check file extension for documents
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf", ".xls", ".xlsx", ".ppt", ".pptx" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("Chỉ hỗ trợ file tài liệu: PDF, DOC, DOCX, TXT, RTF, XLS, XLSX, PPT, PPTX");

            return await UploadFileInternalAsync(file, folder, ResourceType.Raw);
        }

        public async Task<List<UploadResultDto>> UploadMultipleFilesAsync(IList<IFormFile> files, string folder = "general")
        {
            if (files == null || !files.Any())
                throw new ArgumentException("Không có file nào để upload");

            if (files.Count > 10)
                throw new ArgumentException("Chỉ được upload tối đa 10 file cùng lúc");

            var results = new List<UploadResultDto>();

            foreach (var file in files)
            {
                try
                {
                    var result = await UploadFileInternalAsync(file, folder);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    // Log error but continue with other files
                    throw new Exception($"Lỗi upload file '{file.FileName}': {ex.Message}");
                }
            }

            return results;
        }

        public async Task<string> UploadCredentialAsync(IFormFile file, Guid userId, string credentialName)
        {
            // Validate file
            if (file == null || file.Length == 0)
                throw new ArgumentException("File chứng chỉ không hợp lệ");

            // Check file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
                throw new ArgumentException("File không được vượt quá 10MB");

            // Check file extension
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("Chỉ hỗ trợ file PDF, JPG, PNG, DOC, DOCX");

            var folder = $"teacher-credentials/{userId:N}";
            return await UploadFileAsync(file, folder);
        }

        public async Task<bool> DeleteFileAsync(string publicId)
        {
            try
            {
                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);
                return result.Result == "ok";
            }
            catch
            {
                return false;
            }
        }

        private async Task<UploadResultDto> UploadFileInternalAsync(IFormFile file, string folder, ResourceType resourceType = ResourceType.Auto)
        {
            using var stream = file.OpenReadStream();

            if (resourceType == ResourceType.Raw)
            {
                var uploadParams = new RawUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                    throw new Exception($"Lỗi upload file: {uploadResult.Error.Message}");

                return new UploadResultDto
                {
                    Url = uploadResult.SecureUrl.ToString(),
                    PublicId = uploadResult.PublicId,
                    OriginalFileName = file.FileName,
                    FileSize = file.Length,
                    FileType = file.ContentType,
                    UploadedAt = DateTime.UtcNow,
                    Folder = folder
                };
            }
            else
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                    throw new Exception($"Lỗi upload file: {uploadResult.Error.Message}");

                return new UploadResultDto
                {
                    Url = uploadResult.SecureUrl.ToString(),
                    PublicId = uploadResult.PublicId,
                    OriginalFileName = file.FileName,
                    FileSize = file.Length,
                    FileType = file.ContentType,
                    UploadedAt = DateTime.UtcNow,
                    Folder = folder
                };
            }
        }
        public async Task<UploadResultDto> UploadVideoAsync(IFormFile file, string folder = "videos")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid video file");

            // check extension
            var allowedExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                throw new ArgumentException("Only support MP4, MOV, AVI, MKV, WEBM");

            using var stream = file.OpenReadStream();
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false,
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Lỗi upload file: {uploadResult.Error.Message}");

            return new UploadResultDto
            {
                Url = uploadResult.SecureUrl.ToString(),
                PublicId = uploadResult.PublicId,
                OriginalFileName = file.FileName,
                FileSize = file.Length,
                FileType = file.ContentType,
                UploadedAt = DateTime.UtcNow,
                Folder = folder
            };
        }
        public async Task<UploadResultDto> UploadAudioAsync(IFormFile file, string folder = "audios")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File âm thanh không hợp lệ");

            const long maxAudioSize = 20 * 1024 * 1024;
            if (file.Length > maxAudioSize)
                throw new ArgumentException($"File âm thanh không được vượt quá {maxAudioSize / (1024 * 1024)} MB");

            var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                throw new ArgumentException("Chỉ hỗ trợ file âm thanh: MP3, WAV, OGG, FLAC, AAC, M4A");

            using var stream = file.OpenReadStream();

            var uploadParams = new VideoUploadParams()
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false,
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            if (uploadResult.Error != null)
                throw new Exception($"Lỗi upload audio: {uploadResult.Error.Message}");

            return new UploadResultDto
            {
                Url = uploadResult.SecureUrl.ToString(),
                PublicId = uploadResult.PublicId,
                OriginalFileName = file.FileName,
                FileSize = file.Length,
                FileType = file.ContentType,
                UploadedAt = DateTime.UtcNow,
                Folder = folder
            };
        }

        public async Task<ImageUploadResult?> UploadImagesAsync(object fileInput, string folder)
        {
            if (fileInput == null)
                return null;

            Stream? stream = null;
            string fileName;

            if (fileInput is IFormFile formFile)
            {
                stream = formFile.OpenReadStream();
                fileName = formFile.FileName;
            }
            else if (fileInput is string filePath && File.Exists(filePath))
            {
                stream = File.OpenRead(filePath);
                fileName = Path.GetFileName(filePath);
            }
            else
            {
                throw new ArgumentException("Invalid file input. Must be IFormFile or valid file path.");
            }

            using (stream)
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, stream),
                    Folder = folder
                };

                return await _cloudinary.UploadAsync(uploadParams);
            }
        }
    }
}

