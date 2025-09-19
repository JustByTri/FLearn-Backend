using BLL.IServices.Auth;
using BLL.IServices.Teacher;
using BLL.IServices.Upload;
using Common.DTO.Staff;
using Common.DTO.Teacher;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.Teacher
{
    public class TeacherApplicationService : ITeacherApplicationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IEmailService _emailService;
        private readonly ILogger<TeacherApplicationService> _logger;

        public TeacherApplicationService(
            IUnitOfWork unitOfWork,
            ICloudinaryService cloudinaryService,
            IEmailService emailService,
            ILogger<TeacherApplicationService> logger)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<TeacherApplicationDto> CreateApplicationAsync(Guid userId, CreateTeacherApplicationDto dto)
        {
            // Check if user can apply
            if (!await CanUserApplyAsync(userId))
                throw new InvalidOperationException("Bạn đã có đơn ứng tuyển hoặc đã là giáo viên");

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                throw new ArgumentException("Người dùng không tồn tại");

            // Kiểm tra ngôn ngữ có tồn tại không
            var language = await _unitOfWork.Languages.GetByIdAsync(dto.LanguageID);
            if (language == null)
                throw new ArgumentException("Ngôn ngữ không tồn tại");

            // Create application
            var application = new TeacherApplication
            {
                TeacherApplicationID = Guid.NewGuid(),
                UserID = userId,
                LanguageID = dto.LanguageID, // Thêm LanguageID
                Motivation = dto.Motivation,
                AppliedAt = DateTime.UtcNow,
                Status = false, // Pending
                CreatedAt = DateTime.UtcNow,
                RejectionReason = string.Empty
            };

            await _unitOfWork.TeacherApplications.CreateAsync(application);

            // Create credential records
            var credentials = new List<TeacherCredential>();
            foreach (var credentialDto in dto.Credentials)
            {
                var credential = new TeacherCredential
                {
                    TeacherCredentialID = Guid.NewGuid(),
                    UserID = userId,
                    CredentialName = credentialDto.CredentialName,
                    CredentialFileUrl = credentialDto.CredentialFileUrl,
                    ApplicationID = application.TeacherApplicationID,
                    Type = (TeacherCredential.CredentialType)credentialDto.Type,
                    CreatedAt = DateTime.UtcNow
                };

                credentials.Add(credential);
                await _unitOfWork.TeacherCredentials.CreateAsync(credential);
            }

            // Send notification email
            try
            {
                await _emailService.SendTeacherApplicationSubmittedAsync(user.Email!, user.UserName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending application submitted email to {Email}", user.Email);
            }

            return new TeacherApplicationDto
            {
                TeacherApplicationID = application.TeacherApplicationID,
                UserID = application.UserID,
                UserName = user.UserName,
                Email = user.Email!,
                LanguageID = application.LanguageID,
                LanguageName = language.LanguageName,
                Motivation = application.Motivation,
                AppliedAt = application.AppliedAt,
                Status = ApplicationStatus.Pending,
                Credentials = credentials.Select(c => new TeacherCredentialDto
                {
                    TeacherCredentialID = c.TeacherCredentialID,
                    UserID = c.UserID,
                    CredentialName = c.CredentialName,
                    CredentialFileUrl = c.CredentialFileUrl,
                    ApplicationID = c.ApplicationID,
                    Type = c.Type,
                    CreatedAt = c.CreatedAt
                }).ToList(),
                CreatedAt = application.CreatedAt
            };
        }

        public async Task<TeacherApplicationDto?> GetApplicationByUserAsync(Guid userId)
        {
            var application = await _unitOfWork.TeacherApplications.GetLatestApplicationByUserAsync(userId);
            if (application == null) return null;

            var credentials = await _unitOfWork.TeacherCredentials.GetCredentialsByUserAsync(userId);
            var appCredentials = credentials.Where(c => c.ApplicationID == application.TeacherApplicationID).ToList();

            return MapToDto(application, appCredentials);
        }

        // Lấy tất cả đơn ứng tuyển (chỉ cho Admin)
        public async Task<List<TeacherApplicationDto>> GetAllApplicationsAsync()
        {
            var applications = await _unitOfWork.TeacherApplications.GetAllAsync();
            var result = new List<TeacherApplicationDto>();

            foreach (var app in applications)
            {
                var credentials = await _unitOfWork.TeacherCredentials.GetCredentialsByUserAsync(app.UserID);
                var appCredentials = credentials.Where(c => c.ApplicationID == app.TeacherApplicationID).ToList();
                result.Add(MapToDto(app, appCredentials));
            }

            return result.OrderByDescending(x => x.AppliedAt).ToList();
        }

        // Lấy đơn ứng tuyển theo ngôn ngữ của staff
        public async Task<List<TeacherApplicationDto>> GetApplicationsByLanguageAsync(Guid languageId)
        {
            var applications = await _unitOfWork.TeacherApplications.GetApplicationsByLanguageAsync(languageId);
            var result = new List<TeacherApplicationDto>();

            foreach (var app in applications)
            {
                var credentials = await _unitOfWork.TeacherCredentials.GetCredentialsByUserAsync(app.UserID);
                var appCredentials = credentials.Where(c => c.ApplicationID == app.TeacherApplicationID).ToList();
                result.Add(MapToDto(app, appCredentials));
            }

            return result.OrderByDescending(x => x.AppliedAt).ToList();
        }

        // Lấy đơn ứng tuyển đang chờ duyệt theo ngôn ngữ
        public async Task<List<TeacherApplicationDto>> GetPendingApplicationsByLanguageAsync(Guid languageId)
        {
            var applications = await _unitOfWork.TeacherApplications.GetApplicationsByLanguageAsync(languageId);
            var pendingApps = applications.Where(a => !a.Status && string.IsNullOrEmpty(a.RejectionReason)).ToList();

            var result = new List<TeacherApplicationDto>();
            foreach (var app in pendingApps)
            {
                var credentials = await _unitOfWork.TeacherCredentials.GetCredentialsByUserAsync(app.UserID);
                var appCredentials = credentials.Where(c => c.ApplicationID == app.TeacherApplicationID).ToList();
                result.Add(MapToDto(app, appCredentials));
            }

            return result.OrderByDescending(x => x.AppliedAt).ToList();
        }

        public async Task<List<TeacherApplicationDto>> GetPendingApplicationsAsync()
        {
            var applications = await _unitOfWork.TeacherApplications.GetAllAsync();
            var pendingApps = applications.Where(a => !a.Status && string.IsNullOrEmpty(a.RejectionReason)).ToList();

            var result = new List<TeacherApplicationDto>();
            foreach (var app in pendingApps)
            {
                var credentials = await _unitOfWork.TeacherCredentials.GetCredentialsByUserAsync(app.UserID);
                var appCredentials = credentials.Where(c => c.ApplicationID == app.TeacherApplicationID).ToList();
                result.Add(MapToDto(app, appCredentials));
            }

            return result.OrderByDescending(x => x.AppliedAt).ToList();
        }

        public async Task<TeacherApplicationDto?> GetApplicationByIdAsync(Guid applicationId)
        {
            var application = await _unitOfWork.TeacherApplications.GetByIdAsync(applicationId);
            if (application == null) return null;

            var credentials = await _unitOfWork.TeacherCredentials.GetCredentialsByUserAsync(application.UserID);
            var appCredentials = credentials.Where(c => c.ApplicationID == applicationId).ToList();

            return MapToDto(application, appCredentials);
        }

        public async Task<bool> ReviewApplicationAsync(Guid reviewerId, ReviewApplicationDto dto)
        {
            var application = await _unitOfWork.TeacherApplications.GetByIdAsync(dto.ApplicationId);
            if (application == null)
                throw new ArgumentException("Đơn ứng tuyển không tồn tại");

            var reviewer = await _unitOfWork.Users.GetUserWithRolesAsync(reviewerId);
            if (reviewer == null || !reviewer.UserRoles!.Any(ur => ur.Role.Name == "Admin" || ur.Role.Name == "Staff"))
                throw new UnauthorizedAccessException("Chỉ admin hoặc staff mới có thể duyệt đơn ứng tuyển");

            // Kiểm tra quyền của staff với ngôn ngữ
            if (reviewer.UserRoles!.Any(ur => ur.Role.Name == "Staff"))
            {
                var staffLanguages = await _unitOfWork.UserLearningLanguages.GetLanguagesByUserAsync(reviewerId);
                if (!staffLanguages.Any(ul => ul.LanguageID == application.LanguageID))
                {
                    throw new UnauthorizedAccessException("Bạn chỉ có thể duyệt đơn ứng tuyển cho ngôn ngữ được phân công");
                }
            }

            var applicant = await _unitOfWork.Users.GetByIdAsync(application.UserID);
            if (applicant == null)
                throw new ArgumentException("Người ứng tuyển không tồn tại");

            application.Status = dto.IsApproved;
            application.ReviewAt = DateTime.UtcNow;
            application.ReviewedBy = reviewerId;
            application.RejectionReason = dto.IsApproved ? string.Empty : (dto.RejectionReason ?? "Không đáp ứng yêu cầu");

            await _unitOfWork.TeacherApplications.UpdateAsync(application);

            if (dto.IsApproved)
            {
                var teacherRole = await _unitOfWork.Roles.GetByNameAsync("Teacher");
                if (teacherRole != null)
                {
                    var userRole = new UserRole
                    {
                        UserRoleID = Guid.NewGuid(),
                        UserID = application.UserID,
                        RoleID = teacherRole.RoleID
                    };
                    await _unitOfWork.UserRoles.CreateAsync(userRole);
                }
            }

            try
            {
                if (dto.IsApproved)
                {
                    await _emailService.SendTeacherApplicationApprovedAsync(applicant.Email!, applicant.UserName);
                }
                else
                {
                    await _emailService.SendTeacherApplicationRejectedAsync(applicant.Email!, applicant.UserName, application.RejectionReason);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending application review email to {Email}", applicant.Email);
            }

            return true;
        }

        public async Task<bool> CanUserApplyAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetUserWithRolesAsync(userId);
            if (user == null) return false;

            if (user.UserRoles!.Any(ur => ur.Role.Name == "Teacher"))
                return false;

            var existingApplication = await _unitOfWork.TeacherApplications.GetLatestApplicationByUserAsync(userId);
            if (existingApplication != null && (existingApplication.Status || string.IsNullOrEmpty(existingApplication.RejectionReason)))
                return false;

            return true;
        }

        private TeacherApplicationDto MapToDto(TeacherApplication application, List<TeacherCredential> credentials)
        {
            var status = ApplicationStatus.Pending;
            if (application.Status)
                status = ApplicationStatus.Approved;
            else if (!string.IsNullOrEmpty(application.RejectionReason))
                status = ApplicationStatus.Rejected;

            return new TeacherApplicationDto
            {
                TeacherApplicationID = application.TeacherApplicationID,
                UserID = application.UserID,
                UserName = application.User?.UserName ?? "",
                Email = application.User?.Email ?? "",
                LanguageID = application.LanguageID,
                LanguageName = application.Language?.LanguageName ?? "",
                Motivation = application.Motivation,
                AppliedAt = application.AppliedAt,
                SubmitAt = application.SubmitAt,
                ReviewAt = application.ReviewAt == DateTime.MinValue ? null : application.ReviewAt,
                ReviewedBy = application.ReviewedBy == Guid.Empty ? null : application.ReviewedBy,
                RejectionReason = application.RejectionReason,
                Status = status,
                Credentials = credentials.Select(c => new TeacherCredentialDto
                {
                    TeacherCredentialID = c.TeacherCredentialID,
                    UserID = c.UserID,
                    CredentialName = c.CredentialName,
                    CredentialFileUrl = c.CredentialFileUrl,
                    ApplicationID = c.ApplicationID,
                    Type = c.Type,
                    CreatedAt = c.CreatedAt
                }).ToList(),
                CreatedAt = application.CreatedAt
            };
        }
    }
}
