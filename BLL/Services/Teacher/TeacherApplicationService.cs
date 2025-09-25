using BLL.IServices.AI;
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
        private readonly IGeminiService _geminiService;
        public TeacherApplicationService(
            IUnitOfWork unitOfWork,
            ICloudinaryService cloudinaryService,
            IEmailService emailService,
            ILogger<TeacherApplicationService> logger,
            IGeminiService geminiService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _emailService = emailService;
            _logger = logger;
            _geminiService = geminiService;
        }

        public async Task<TeacherApplicationDto> CreateApplicationAsync(Guid userId, CreateTeacherApplicationDto dto)
        {
            try
            {

                if (!await CanUserApplyAsync(userId))
                    throw new InvalidOperationException("Bạn đã có đơn ứng tuyển đang chờ duyệt hoặc đã là giáo viên");

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    throw new ArgumentException("Người dùng không tồn tại");

                var language = await _unitOfWork.Languages.GetByIdAsync(dto.LanguageID);
                if (language == null)
                    throw new ArgumentException("Ngôn ngữ không tồn tại");

                var supportedLanguages = new[] { "EN", "ZH", "JP" };
                if (!supportedLanguages.Contains(language.LanguageCode))
                    throw new ArgumentException("Hiện tại chúng tôi chỉ nhận đơn ứng tuyển cho tiếng Anh, tiếng Trung và tiếng Nhật");

                var application = new TeacherApplication
                {
                    TeacherApplicationID = Guid.NewGuid(),
                    UserID = userId,
                    LanguageID = dto.LanguageID,
                    Motivation = dto.Motivation,
                    TeachingExperience = dto.TeachingExperience ?? string.Empty,
                    TeachingLevel = dto.TeachingLevel ?? "All Levels",
                    Specialization = dto.Specialization ?? string.Empty,

                    AppliedAt = DateTime.UtcNow,
                    Status = false,
                    CreatedAt = DateTime.UtcNow,
                    RejectionReason = string.Empty
                };

                await _unitOfWork.TeacherApplications.CreateAsync(application);


                var credentials = new List<TeacherCredential>();
                if (dto.Credentials?.Any() == true)
                {
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
                }

                await _unitOfWork.SaveChangesAsync();


                try
                {
                    await _emailService.SendTeacherApplicationSubmittedAsync(user.Email!, user.UserName);
                    _logger.LogInformation("Sent application submitted email to {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending application submitted email to {Email}", user.Email);
                }

                return await MapToDtoAsync(application, credentials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating teacher application for user {UserId}", userId);
                throw;
            }
        }

        public async Task<TeacherApplicationDto?> GetApplicationByUserAsync(Guid userId)
        {
            try
            {
                var application = await _unitOfWork.TeacherApplications.GetLatestApplicationByUserAsync(userId);
                if (application == null) return null;

                var credentials = await _unitOfWork.TeacherCredentials.GetCredentialsByUserAsync(userId);
                var appCredentials = credentials.Where(c => c.ApplicationID == application.TeacherApplicationID).ToList();

                return await MapToDtoAsync(application, appCredentials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<TeacherApplicationDto>> GetAllApplicationsAsync()
        {
            try
            {
                var applications = await _unitOfWork.TeacherApplications.GetAllAsync();
                var result = new List<TeacherApplicationDto>();

                foreach (var app in applications)
                {
                    var credentials = await _unitOfWork.TeacherCredentials.GetCredentialsByUserAsync(app.UserID);
                    var appCredentials = credentials.Where(c => c.ApplicationID == app.TeacherApplicationID).ToList();
                    result.Add(await MapToDtoAsync(app, appCredentials));
                }

                return result.OrderByDescending(x => x.AppliedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all applications");
                throw;
            }
        }


        public async Task<List<TeacherApplicationDto>> GetApplicationsByLanguageAsync(Guid languageId)
        {
            try
            {
                var applications = await _unitOfWork.TeacherApplications.GetByLanguageAsync(languageId);
                var result = new List<TeacherApplicationDto>();

                foreach (var app in applications)
                {
                    var credentials = await _unitOfWork.TeacherCredentials.GetCredentialsByUserAsync(app.UserID);
                    var appCredentials = credentials.Where(c => c.ApplicationID == app.TeacherApplicationID).ToList();
                    result.Add(await MapToDtoAsync(app, appCredentials));
                }

                return result.OrderByDescending(x => x.AppliedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications for language {LanguageId}", languageId);
                throw;
            }
        }

        public async Task<List<TeacherApplicationDto>> GetPendingApplicationsByLanguageAsync(Guid languageId)
        {
            try
            {
                var applications = await _unitOfWork.TeacherApplications.GetApplicationsByLanguageAsync(languageId);
                var pendingApps = applications.Where(a => !a.Status && string.IsNullOrEmpty(a.RejectionReason)).ToList();

                var result = new List<TeacherApplicationDto>();
                foreach (var app in pendingApps)
                {
                    var credentials = await _unitOfWork.TeacherCredentials.GetCredentialsByUserAsync(app.UserID);
                    var appCredentials = credentials.Where(c => c.ApplicationID == app.TeacherApplicationID).ToList();
                    result.Add(await MapToDtoAsync(app, appCredentials));
                }

                return result.OrderByDescending(x => x.AppliedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending applications for language {LanguageId}", languageId);
                throw;
            }
        }

        public async Task<List<TeacherApplicationDto>> GetPendingApplicationsAsync()
        {
            try
            {
                var applications = await _unitOfWork.TeacherApplications.GetAllAsync();
                var pendingApps = applications.Where(a => !a.Status && string.IsNullOrEmpty(a.RejectionReason)).ToList();

                var result = new List<TeacherApplicationDto>();
                foreach (var app in pendingApps)
                {
                    var credentials = await _unitOfWork.TeacherCredentials.GetCredentialsByUserAsync(app.UserID);
                    var appCredentials = credentials.Where(c => c.ApplicationID == app.TeacherApplicationID).ToList();
                    result.Add(await MapToDtoAsync(app, appCredentials));
                }

                return result.OrderByDescending(x => x.AppliedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending applications");
                throw;
            }
        }

        public async Task<TeacherApplicationDto?> GetApplicationByIdAsync(Guid applicationId)
        {
            try
            {
                var application = await _unitOfWork.TeacherApplications.GetByIdAsync(applicationId);
                if (application == null) return null;

                var credentials = await _unitOfWork.TeacherCredentials.GetCredentialsByUserAsync(application.UserID);
                var appCredentials = credentials.Where(c => c.ApplicationID == applicationId).ToList();

                return await MapToDtoAsync(application, appCredentials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application {ApplicationId}", applicationId);
                throw;
            }
        }


        public async Task<bool> ReviewApplicationAsync(Guid reviewerId, ReviewApplicationDto dto)
        {
            try
            {
                var application = await _unitOfWork.TeacherApplications.GetByIdAsync(dto.ApplicationId);
                if (application == null)
                    throw new ArgumentException("Đơn ứng tuyển không tồn tại");

                var reviewer = await _unitOfWork.Users.GetUserWithRolesAsync(reviewerId);
                if (reviewer == null || !reviewer.UserRoles!.Any(ur => ur.Role.Name == "Admin" || ur.Role.Name == "Staff"))
                    throw new UnauthorizedAccessException("Chỉ Admin hoặc Staff mới có thể duyệt đơn ứng tuyển");


                if (reviewer.UserRoles!.Any(ur => ur.Role.Name == "Staff" && !ur.Role.Name.Contains("Admin")))
                {
                    var staffLanguages = await _unitOfWork.UserLearningLanguages.GetLanguagesByUserAsync(reviewerId);
                    if (!staffLanguages.Any(ul => ul.LanguageID == application.LanguageID))
                    {
                        throw new UnauthorizedAccessException("Bạn chỉ có thể duyệt đơn ứng tuyển cho ngôn ngữ được phân công quản lý");
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

                        var existingRole = await _unitOfWork.UserRoles.GetUserRoleAsync(application.UserID, teacherRole.RoleID);
                        if (existingRole == null)
                        {
                            var userRole = new UserRole
                            {
                                UserRoleID = Guid.NewGuid(),
                                UserID = application.UserID,
                                RoleID = teacherRole.RoleID
                            };
                            await _unitOfWork.UserRoles.CreateAsync(userRole);


                            var existingUserLanguage = await _unitOfWork.UserLearningLanguages.GetUserLearningLanguageAsync(application.UserID, application.LanguageID);
                            if (existingUserLanguage == null)
                            {
                                await _unitOfWork.UserLearningLanguages.CreateAsync(new UserLearningLanguage
                                {
                                    UserLearningLanguageID = Guid.NewGuid(),
                                    UserID = application.UserID,
                                    LanguageID = application.LanguageID
                                });
                            }
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync();


                try
                {
                    if (dto.IsApproved)
                    {
                        await _emailService.SendTeacherApplicationApprovedAsync(applicant.Email!, applicant.UserName);
                        _logger.LogInformation("Sent approval email to {Email}", applicant.Email);
                    }
                    else
                    {
                        await _emailService.SendTeacherApplicationRejectedAsync(applicant.Email!, applicant.UserName, application.RejectionReason);
                        _logger.LogInformation("Sent rejection email to {Email}", applicant.Email);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending application review email to {Email}", applicant.Email);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing application {ApplicationId}", dto.ApplicationId);
                throw;
            }
        }


        public async Task<bool> CanUserApplyAsync(Guid userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetUserWithRolesAsync(userId);
                if (user == null) return false;


                if (user.UserRoles!.Any(ur => ur.Role.Name == "Teacher"))
                {
                    _logger.LogInformation("User {UserId} already has Teacher role", userId);
                    return false;
                }


                var existingApplication = await _unitOfWork.TeacherApplications.GetLatestApplicationByUserAsync(userId);
                if (existingApplication != null)
                {

                    if (existingApplication.Status)
                    {
                        _logger.LogInformation("User {UserId} already has approved application", userId);
                        return false;
                    }


                    if (string.IsNullOrEmpty(existingApplication.RejectionReason))
                    {
                        _logger.LogInformation("User {UserId} has pending application", userId);
                        return false;
                    }


                    if (!string.IsNullOrEmpty(existingApplication.RejectionReason) &&
                        existingApplication.ReviewAt > DateTime.UtcNow.AddDays(-5))
                    {
                        _logger.LogInformation("User {UserId} rejected application is too recent", userId);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} can apply", userId);
                return false;
            }
        }


        public async Task<Dictionary<string, object>> GetApplicationStatsByLanguageAsync(Guid? languageId = null)
        {
            try
            {
                var stats = new Dictionary<string, object>();

                if (languageId.HasValue)
                {

                    var applications = await _unitOfWork.TeacherApplications.GetByLanguageAsync(languageId.Value);
                    var language = await _unitOfWork.Languages.GetByIdAsync(languageId.Value);

                    stats["languageId"] = languageId.Value;
                    stats["languageName"] = language?.LanguageName ?? "Unknown";
                    stats["totalApplications"] = applications.Count;
                    stats["pendingApplications"] = applications.Count(a => !a.Status && string.IsNullOrEmpty(a.RejectionReason));
                    stats["approvedApplications"] = applications.Count(a => a.Status);
                    stats["rejectedApplications"] = applications.Count(a => !a.Status && !string.IsNullOrEmpty(a.RejectionReason));
                }
                else
                {

                    var allApplications = await _unitOfWork.TeacherApplications.GetAllAsync();
                    stats["totalApplications"] = allApplications.Count;
                    stats["pendingApplications"] = allApplications.Count(a => !a.Status && string.IsNullOrEmpty(a.RejectionReason));
                    stats["approvedApplications"] = allApplications.Count(a => a.Status);
                    stats["rejectedApplications"] = allApplications.Count(a => !a.Status && !string.IsNullOrEmpty(a.RejectionReason));
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application stats for language {LanguageId}", languageId);
                throw;
            }
        }


        private async Task<TeacherApplicationDto> MapToDtoAsync(TeacherApplication application, List<TeacherCredential> credentials)
        {
            try
            {
                var status = ApplicationStatus.Pending;
                if (application.Status)
                    status = ApplicationStatus.Approved;
                else if (!string.IsNullOrEmpty(application.RejectionReason))
                    status = ApplicationStatus.Rejected;


                var user = application.User ?? await _unitOfWork.Users.GetByIdAsync(application.UserID);


                var language = application.Language ?? await _unitOfWork.Languages.GetByIdAsync(application.LanguageID);

                return new TeacherApplicationDto
                {
                    TeacherApplicationID = application.TeacherApplicationID,
                    UserID = application.UserID,
                    UserName = user?.UserName ?? "",
                    Email = user?.Email ?? "",
                    LanguageID = application.LanguageID,
                    LanguageName = language?.LanguageName ?? "",
                    Motivation = application.Motivation,
                    TeachingExperience = application.TeachingExperience ?? "",
                    TeachingLevel = application.TeachingLevel ?? "",
                    Specialization = application.Specialization ?? "",
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping TeacherApplication to DTO");
                throw;
            }
        }
        public async Task<TeacherQualificationAnalysisDto> AnalyzeQualificationsAsync(Guid applicationId)
        {
            try
            {
                var application = await GetApplicationByIdAsync(applicationId); if (application == null) throw new ArgumentException("Đơn ứng tuyển không tồn tại");
                var credentials = application.Credentials ?? new List<TeacherCredentialDto>();

                
                var analysis = await _geminiService.AnalyzeTeacherQualificationsAsync(application, credentials);

                _logger.LogInformation("AI analysis completed for application {ApplicationId}. Suggested levels: {Levels}",
                    applicationId, string.Join(", ", analysis.SuggestedTeachingLevels));

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing qualifications for application {ApplicationId}", applicationId);
                throw;
            }
        }
        public async Task<TeacherQualificationAnalysisDto> GetQualificationAnalysisForReviewAsync(Guid applicationId, Guid reviewerId)
        {
            try
            {
            
                var reviewer = await _unitOfWork.Users.GetUserWithRolesAsync(reviewerId);
                if (reviewer == null || !reviewer.UserRoles!.Any(ur => ur.Role.Name == "Admin" || ur.Role.Name == "Staff"))
                    throw new UnauthorizedAccessException("Chỉ Admin hoặc Staff mới có thể xem phân tích AI");

                var application = await GetApplicationByIdAsync(applicationId);
                if (application == null)
                    throw new ArgumentException("Đơn ứng tuyển không tồn tại");

            
                if (reviewer.UserRoles!.Any(ur => ur.Role.Name == "Staff" && !ur.Role.Name.Contains("Admin")))
                {
                    var staffLanguages = await _unitOfWork.UserLearningLanguages.GetLanguagesByUserAsync(reviewerId);
                    if (!staffLanguages.Any(ul => ul.LanguageID == application.LanguageID))
                        throw new UnauthorizedAccessException("Bạn chỉ có thể xem phân tích cho ngôn ngữ được phân công");
                }

                return await AnalyzeQualificationsAsync(applicationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting qualification analysis for review {ApplicationId} by {ReviewerId}", applicationId, reviewerId);
                throw;
            }
        }
    }
}
