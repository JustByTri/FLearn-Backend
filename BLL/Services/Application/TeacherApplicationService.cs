using BLL.IServices.Application;
using BLL.IServices.Auth;
using BLL.IServices.Upload;
using Common.DTO.ApiResponse;
using Common.DTO.Application.Request;
using Common.DTO.Application.Response;
using Common.DTO.Application.Uploads;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Application
{
    public class TeacherApplicationService : ITeacherApplicationService
    {
        private readonly IUnitOfWork _unit;
        private readonly ICloudinaryService _cloudinary;
        private readonly IEmailService _email;
        public TeacherApplicationService(IUnitOfWork unit, ICloudinaryService cloudinary, IEmailService email)
        {
            _unit = unit;
            _cloudinary = cloudinary;
            _email = email;
        }
        public async Task<BaseResponse<ApplicationResponse>> ApproveApplicationAsync(Guid userId, Guid applicationId)
        {
            try
            {
                var selectedManager = await _unit.ManagerLanguages.Query()
                    .Include(s => s.User)
                    .Where(s => s.UserId == userId)
                    .FirstOrDefaultAsync();

                if (selectedManager == null)
                    return BaseResponse<ApplicationResponse>.Fail(errors: new object(), "Access denied.", 403);

                var existingApp = await _unit.TeacherApplications.Query()
                    .Include(a => a.Language)
                    .Include(a => a.User)
                    .Include(a => a.Certificates)
                        .ThenInclude(c => c.CertificateType)
                    .Where(a => a.ApplicationID == applicationId &&
                                a.Language.LanguageID == selectedManager.LanguageId)
                    .FirstOrDefaultAsync();

                if (existingApp == null)
                    return BaseResponse<ApplicationResponse>.Fail("Application not found or you are not allowed to approve this application.");

                if (existingApp.Status != ApplicationStatus.Pending)
                    return BaseResponse<ApplicationResponse>.Fail("Only pending applications can be approved.");

                existingApp.Status = ApplicationStatus.Approved;
                existingApp.ReviewedBy = selectedManager.ManagerId;
                existingApp.ReviewedAt = TimeHelper.GetVietnamTime();

                await _unit.SaveChangesAsync();

                var teacherRole = await _unit.Roles.Query()
                    .Where(r => r.Name == "Teacher")
                    .FirstOrDefaultAsync();

                if (teacherRole != null)
                {
                    var userRole = new UserRole
                    {
                        UserRoleID = Guid.NewGuid(),
                        RoleID = teacherRole.RoleID,
                        UserID = existingApp.UserID,
                        CreatedAt = TimeHelper.GetVietnamTime(),
                        UpdatedAt = TimeHelper.GetVietnamTime(),
                    };

                    await _unit.UserRoles.CreateAsync(userRole);
                }

                var teacherProfile = new TeacherProfile
                {
                    TeacherId = Guid.NewGuid(),
                    UserId = existingApp.UserID,
                    LanguageId = existingApp.LanguageID,
                    FullName = existingApp.FullName,
                    BirthDate = existingApp.BirthDate,
                    Bio = existingApp.Bio,
                    Avatar = existingApp.Avatar,
                    Email = existingApp.Email,
                    PhoneNumber = existingApp.PhoneNumber,
                    ProficiencyCode = existingApp.ProficiencyCode,
                    ProficiencyOrder = existingApp.ProficiencyOrder,
                    MeetingUrl = existingApp.MeetingUrl,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime(),
                };

                await _unit.TeacherProfiles.CreateAsync(teacherProfile);

                BackgroundJob.Enqueue(() => _email.SendTeacherApplicationApprovedAsync(existingApp.User.Email, existingApp.User.UserName));
                BackgroundJob.Enqueue(() => AssignTeacherProgramsAsync(teacherProfile.TeacherId));

                var response = new ApplicationResponse
                {
                    ApplicationID = existingApp.ApplicationID,
                    Language = existingApp.Language?.LanguageName,
                    FullName = existingApp.FullName,
                    DateOfBirth = existingApp.BirthDate.ToString("dd/MM/yyyy"),
                    Bio = existingApp.Bio,
                    Avatar = existingApp.Avatar,
                    Email = existingApp.Email,
                    PhoneNumber = existingApp.PhoneNumber,
                    MeetingUrl = existingApp.MeetingUrl,
                    TeachingExperience = existingApp.TeachingExperience,
                    Status = existingApp.Status.ToString(),
                    SubmittedAt = existingApp.SubmittedAt.ToString("dd/MM/yyyy"),
                    ReviewedAt = existingApp.ReviewedAt.ToString("dd/MM/yyyy"),
                    RejectionReason = existingApp.RejectionReason,
                    ProficiencyCode = existingApp.ProficiencyCode,
                    Submitter = existingApp.User == null ? null : new UserResponse
                    {
                        UserId = existingApp.User.UserID,
                        FullName = existingApp.User.FullName ?? existingApp.FullName,
                        Email = existingApp.User.Email,
                        PhoneNumber = existingApp.PhoneNumber
                    },
                    Reviewer = selectedManager.User == null ? null : new UserResponse
                    {
                        UserId = selectedManager.User.UserID,
                        FullName = selectedManager.User.FullName ?? selectedManager.User.UserName,
                        Email = selectedManager.User.Email
                    },
                    Certificates = existingApp.Certificates.Select(cert => new ApplicationCertTypeResponse
                    {
                        Id = cert.ApplicationCertTypeId,
                        CertificateImageUrl = cert.CertificateImageUrl,
                        CertificateName = cert.CertificateType?.Name
                    }).ToList()
                };

                return BaseResponse<ApplicationResponse>.Success(response, "Application approved successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<ApplicationResponse>.Error($"Unexpected error: {ex.Message}");
            }
        }
        public async Task AssignTeacherProgramsAsync(Guid teacherId)
        {
            try
            {
                var teacher = await _unit.TeacherProfiles.Query()
                    .OrderBy(t => t.CreatedAt)
                    .Include(t => t.Language)
                    .Include(t => t.TeacherProgramAssignments)
                    .FirstOrDefaultAsync(t => t.TeacherId == teacherId);

                if (teacher == null)
                {
                    Console.WriteLine($"[WARN] Application or teacher not found (TeacherID={teacherId}).");
                    return;
                }

                if (teacher.TeacherProgramAssignments.Any())
                {
                    Console.WriteLine($"[INFO] Teacher {teacherId} already has assigned programs — skipping.");
                    return;
                }

                var programs = await _unit.Programs.Query()
                    .OrderBy(p => p.CreatedAt)
                    .Include(p => p.Levels)
                    .Where(p => p.LanguageId == teacher.LanguageId && p.Status)
                    .ToListAsync();

                if (!programs.Any())
                {
                    Console.WriteLine($"[WARN] No active programs found for language {teacher.Language.LanguageName}.");
                    return;
                }

                foreach (var program in programs)
                {
                    var eligibleLevels = program.Levels
                        .Where(l => l.OrderIndex <= teacher.ProficiencyOrder && l.Status)
                        .ToList();

                    if (!eligibleLevels.Any())
                    {
                        Console.WriteLine($"[INFO] Program '{program.Name}' has no eligible levels (≤ {teacher.ProficiencyOrder}).");
                        continue;
                    }

                    foreach (var level in eligibleLevels)
                    {

                        var assignment = new TeacherProgramAssignment
                        {
                            ProgramAssignmentId = Guid.NewGuid(),
                            TeacherId = teacherId,
                            ProgramId = program.ProgramId,
                            LevelId = level.LevelId,
                            AssignedAt = TimeHelper.GetVietnamTime(),
                            Status = true,
                        };

                        await _unit.TeacherProgramAssignments.CreateAsync(assignment);
                        Console.WriteLine($"[SUCCESS] Assigned Teacher={teacherId} → Program='{program.Name}', Level='{level.Name}'.");
                    }
                }

                await _unit.SaveChangesAsync();

                var walletExists = await _unit.Wallets.Query().AnyAsync(w => w.TeacherId == teacher.TeacherId);

                if (!walletExists)
                {
                    var newWallet = new Wallet
                    {
                        WalletId = Guid.NewGuid(),
                        Name = teacher.FullName + " - Teacher Wallet",
                        TeacherId = teacher.TeacherId,
                        OwnerType = OwnerType.Teacher,
                        CreatedAt = TimeHelper.GetVietnamTime(),
                        UpdatedAt = TimeHelper.GetVietnamTime(),
                    };

                    await _unit.Wallets.CreateAsync(newWallet);

                    await _unit.SaveChangesAsync();
                    Console.WriteLine($"[SUCCESS] Program assignment completed for Teacher={teacherId}.");
                }
                else
                {
                    Console.WriteLine($"[INFO] Wallet already exists for Teacher={teacherId}.");
                }

                Console.WriteLine($"[SUCCESS] Completed program assignment & wallet creation for Teacher={teacherId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to assign programs: {ex.Message}");
            }
        }
        public async Task<BaseResponse<ApplicationResponse>> CreateApplicationAsync(Guid userId, ApplicationRequest applicationRequest)
        {
            var selectedUser = await _unit.Users.GetByIdAsync(userId);

            if (selectedUser == null)
                return BaseResponse<ApplicationResponse>.Fail(errors: new object(), "Access denied.", 403);

            var existingApp = await _unit.TeacherApplications
                .FindAsync(a => a.UserID == userId &&
                               (a.Status == ApplicationStatus.Pending || a.Status == ApplicationStatus.Approved));

            if (existingApp != null)
            {
                return BaseResponse<ApplicationResponse>.Fail(errors: new object(), "You have already submitted an application.", 400);
            }

            var selectedLang = await _unit.Languages.FindByLanguageCodeAsync(applicationRequest.LangCode);

            if (selectedLang == null)
            {
                return BaseResponse<ApplicationResponse>.Fail(errors: new object(), "Language does not exist.", 404);
            }

            var certificateTypeIds = applicationRequest.CertificateTypeIds
                .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(x => x.Trim())
                .ToArray();

            if (applicationRequest.CertificateImages.Length != certificateTypeIds.Length)
            {
                return BaseResponse<ApplicationResponse>.Fail(errors: new object(), "CertificateImages count must match CertificateTypeIds count.", 400);
            }

            foreach (var idStr in certificateTypeIds)
            {
                if (!Guid.TryParse(idStr, out var certId))
                    return BaseResponse<ApplicationResponse>.Fail(errors: new object(), $"Invalid CertificateTypeId: {idStr}", 400);

                var certType = await _unit.CertificateTypes.GetByIdAsync(certId);
                if (certType == null)
                    return BaseResponse<ApplicationResponse>.Fail(errors: new object(), $"CertificateTypeId {idStr} does not exist.", 404);
            }

            var proficiencyOrder = GetProficiencyOrder(applicationRequest.LangCode, applicationRequest.ProficiencyCode);

            var application = new TeacherApplication
            {
                ApplicationID = Guid.NewGuid(),
                UserID = selectedUser.UserID,
                LanguageID = selectedLang.LanguageID,
                FullName = applicationRequest.FullName,
                BirthDate = applicationRequest.BirthDate,
                Bio = applicationRequest.Bio,
                Email = applicationRequest.Email,
                PhoneNumber = applicationRequest.PhoneNumber,
                MeetingUrl = applicationRequest.MeetingUrl,
                TeachingExperience = applicationRequest.TeachingExperience,
                ProficiencyCode = applicationRequest.ProficiencyCode,
                ProficiencyOrder = proficiencyOrder,
                Status = ApplicationStatus.Pending,
                SubmittedAt = TimeHelper.GetVietnamTime()
            };

            await _unit.TeacherApplications.CreateAsync(application);

            var avatarPath = await SaveFileAsync(applicationRequest.Avatar, "avatars");
            var certificateInfos = new List<CertificateUploadInfo>();
            for (int i = 0; i < applicationRequest.CertificateImages.Length; i++)
            {
                var certPath = await SaveFileAsync(applicationRequest.CertificateImages[i], "certificates");
                certificateInfos.Add(new CertificateUploadInfo
                {
                    CertificateTypeId = certificateTypeIds[i],
                    CertificateImagePath = certPath
                });
            }

            var uploadInfo = new ApplicationUploadInfo
            {
                ApplicationId = application.ApplicationID,
                AvatarPath = avatarPath,
                Certificates = certificateInfos
            };

            BackgroundJob.Enqueue(() => ProcessApplicationUploadsAsync(uploadInfo));

            BackgroundJob.Enqueue(() => SendApplicationEmailAsync(selectedUser.Email, selectedUser.UserName));

            return BaseResponse<ApplicationResponse>.Success(
                new ApplicationResponse
                {
                    ApplicationID = application.ApplicationID,
                    Status = application.Status.ToString(),
                    SubmittedAt = application.SubmittedAt.ToString("dd/MM/yyyy HH:mm:ss")
                },
                "Your application has been submitted and is being processed in background.",
                201
            );
        }
        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine("uploads", folder, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return filePath;
        }
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessApplicationUploadsAsync(ApplicationUploadInfo info)
        {
            try
            {
                var application = await _unit.TeacherApplications.GetByIdAsync(info.ApplicationId);
                if (application == null)
                    throw new Exception("Application not found.");
                if (File.Exists(info.AvatarPath))
                {
                    var avatarUpload = await _cloudinary.UploadImagesAsync(info.AvatarPath, "avatars");
                    if (avatarUpload != null && avatarUpload.Url != null)
                        application.Avatar = avatarUpload.Url.ToString();

                    File.Delete(info.AvatarPath);
                }
                foreach (var cert in info.Certificates)
                {
                    if (!File.Exists(cert.CertificateImagePath))
                        continue;

                    var upload = await _cloudinary.UploadImagesAsync(cert.CertificateImagePath, "certificates");
                    if (upload != null && upload.Url != null)
                    {
                        await _unit.ApplicationCertTypes.CreateAsync(new ApplicationCertType
                        {
                            ApplicationCertTypeId = Guid.NewGuid(),
                            ApplicationId = info.ApplicationId,
                            CertificateTypeId = Guid.Parse(cert.CertificateTypeId),
                            CertificateImageUrl = upload.Url.ToString(),
                            CertificateImagePublicId = upload.PublicId,
                        });
                    }
                    File.Delete(cert.CertificateImagePath);
                }

                await _unit.SaveChangesAsync();
                Console.WriteLine($"***[DEBUG]***[SUCCESS] Upload background completed for ApplicationID: {info.ApplicationId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"***[DEBUG]***[FAILED] Background upload failed for ApplicationID: {info.ApplicationId} — {ex.Message}");
            }
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task SendApplicationEmailAsync(string toEmail, string userName)
        {
            try
            {
                await _email.SendTeacherApplicationSubmittedAsync(toEmail, userName);
                Console.WriteLine($"***[DEBUG]***[SUCCESS] Email sent to {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"***[DEBUG]***[FAILED] Failed to send email to {toEmail}: {ex.Message}");
            }
        }
        public async Task<PagedResponse<IEnumerable<ApplicationResponse>>> GetApplicationAsync(Guid userId, PagingRequest request, string status)
        {
            try
            {
                var selectedUser = await _unit.Users.Query()
                    .Include(u => u.ManagerLanguage)
                    .Where(u => u.UserID == userId)
                    .FirstOrDefaultAsync();

                if (selectedUser == null)
                    return PagedResponse<IEnumerable<ApplicationResponse>>.Fail(errors: new object(), "Access denied.", 403);

                if (selectedUser.ManagerLanguage == null)
                    return PagedResponse<IEnumerable<ApplicationResponse>>.Fail(null, "Manager language is not assigned.", 400);

                var query = _unit.TeacherApplications.Query()
                    .Include(a => a.Language)
                    .Include(a => a.User)
                    .Include(a => a.Certificates)
                        .ThenInclude(c => c.CertificateType)
                    .Where(a => a.Language.LanguageID == selectedUser.ManagerLanguage.LanguageId);

                if (!string.IsNullOrWhiteSpace(status) &&
                    Enum.TryParse<ApplicationStatus>(status, true, out var parsedStatus))
                {
                    query = query.Where(a => a.Status == parsedStatus);
                }

                query = query.OrderByDescending(a => a.SubmittedAt);

                var totalItems = await query.CountAsync();

                var applications = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var responses = new List<ApplicationResponse>();

                foreach (var teacherApp in applications)
                {
                    User reviewer = null;
                    if (teacherApp.ReviewedBy != null)
                    {
                        var managerLang = await _unit.ManagerLanguages.Query()
                            .Include(s => s.User)
                            .Where(s => s.ManagerId == teacherApp.ReviewedBy)
                            .FirstOrDefaultAsync();
                        reviewer = managerLang?.User;
                    }

                    responses.Add(new ApplicationResponse
                    {
                        ApplicationID = teacherApp.ApplicationID,
                        Language = teacherApp.Language.LanguageName,
                        FullName = teacherApp.FullName,
                        DateOfBirth = teacherApp.BirthDate.ToString("dd/MM/yyyy"),
                        Bio = teacherApp.Bio,
                        Avatar = teacherApp.Avatar,
                        Email = teacherApp.Email,
                        PhoneNumber = teacherApp.PhoneNumber,
                        TeachingExperience = teacherApp.TeachingExperience,
                        MeetingUrl = teacherApp.MeetingUrl,
                        Status = teacherApp.Status.ToString(),
                        SubmittedAt = teacherApp.SubmittedAt.ToString("dd/MM/yyyy HH:mm"),
                        ReviewedAt = (teacherApp.ReviewedAt != DateTime.MinValue)
                            ? teacherApp.ReviewedAt.ToString("dd/MM/yyyy HH:mm") : string.Empty,
                        RejectionReason = teacherApp.RejectionReason,
                        ProficiencyCode = teacherApp.ProficiencyCode,
                        Submitter = teacherApp.User == null ? null : new UserResponse
                        {
                            UserId = teacherApp.User.UserID,
                            FullName = teacherApp.User?.FullName ?? teacherApp.FullName,
                            PhoneNumber = teacherApp.PhoneNumber,
                            Email = teacherApp.User?.Email ?? teacherApp.Email
                        },
                        Reviewer = reviewer == null ? null : new UserResponse
                        {
                            UserId = reviewer.UserID,
                            FullName = reviewer.FullName ?? reviewer.UserName,
                            Email = reviewer.Email
                        },
                        Certificates = teacherApp.Certificates.Select(cert => new ApplicationCertTypeResponse
                        {
                            Id = cert.ApplicationCertTypeId,
                            CertificateImageUrl = cert.CertificateImageUrl,
                            CertificateName = cert.CertificateType != null ? cert.CertificateType.Name : null
                        }).ToList()
                    });
                }

                return PagedResponse<IEnumerable<ApplicationResponse>>.Success(
                    responses,
                    request.Page,
                    request.PageSize,
                    totalItems,
                    "Applications retrieved successfully."
                );
            }
            catch (Exception ex)
            {
                return PagedResponse<IEnumerable<ApplicationResponse>>.Error($"Unexpected error: {ex.Message}");
            }
        }
        public async Task<PagedResponse<IEnumerable<ApplicationResponse>>> GetMyApplicationAsync(Guid userId, PagingRequest request, string? status)
        {
            try
            {
                var selectedUser = await _unit.Users.GetByIdAsync(userId);

                if (selectedUser == null)
                    return PagedResponse<IEnumerable<ApplicationResponse>>.Fail(errors: new object(), "Access denied.", 403);

                var query = _unit.TeacherApplications.Query()
                    .Include(a => a.Language)
                    .Include(a => a.User)
                    .Include(a => a.Certificates)
                        .ThenInclude(c => c.CertificateType)
                    .Where(a => a.UserID == selectedUser.UserID);

                if (!string.IsNullOrWhiteSpace(status) &&
                    Enum.TryParse<ApplicationStatus>(status, true, out var parsedStatus))
                {
                    query = query.Where(a => a.Status == parsedStatus);
                }

                query = query.OrderByDescending(a => a.SubmittedAt);

                var totalItems = await query.CountAsync();

                var applications = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var responses = new List<ApplicationResponse>();

                foreach (var teacherApp in applications)
                {
                    User reviewer = null;
                    if (teacherApp.ReviewedBy != null)
                    {
                        var managerLang = await _unit.ManagerLanguages.Query()
                            .Include(s => s.User)
                            .Where(s => s.ManagerId == teacherApp.ReviewedBy)
                            .FirstOrDefaultAsync();
                        reviewer = managerLang?.User;
                    }

                    responses.Add(new ApplicationResponse
                    {
                        ApplicationID = teacherApp.ApplicationID,
                        Language = teacherApp.Language.LanguageName,
                        FullName = teacherApp.FullName,
                        DateOfBirth = teacherApp.BirthDate.ToString("dd/MM/yyyy"),
                        Bio = teacherApp.Bio,
                        Avatar = teacherApp.Avatar,
                        Email = teacherApp.Email,
                        PhoneNumber = teacherApp.PhoneNumber,
                        TeachingExperience = teacherApp.TeachingExperience,
                        MeetingUrl = teacherApp.MeetingUrl,
                        Status = teacherApp.Status.ToString(),
                        SubmittedAt = teacherApp.SubmittedAt.ToString("dd/MM/yyyy HH:mm"),
                        ReviewedAt = (teacherApp.ReviewedAt != DateTime.MinValue)
                            ? teacherApp.ReviewedAt.ToString("dd/MM/yyyy HH:mm") : string.Empty,
                        RejectionReason = teacherApp.RejectionReason,
                        ProficiencyCode = teacherApp.ProficiencyCode,
                        Submitter = teacherApp.User == null ? null : new UserResponse
                        {
                            UserId = teacherApp.User.UserID,
                            FullName = teacherApp.User?.FullName ?? teacherApp.FullName,
                            PhoneNumber = teacherApp.PhoneNumber,
                            Email = teacherApp.User?.Email ?? teacherApp.Email
                        },
                        Reviewer = reviewer == null ? null : new UserResponse
                        {
                            UserId = reviewer.UserID,
                            FullName = reviewer.FullName ?? reviewer.UserName,
                            Email = reviewer.Email
                        },
                        Certificates = teacherApp.Certificates.Select(cert => new ApplicationCertTypeResponse
                        {
                            Id = cert.ApplicationCertTypeId,
                            CertificateImageUrl = cert.CertificateImageUrl,
                            CertificateName = cert.CertificateType != null ? cert.CertificateType.Name : null
                        }).ToList()
                    });
                }

                return PagedResponse<IEnumerable<ApplicationResponse>>.Success(
                    responses,
                    request.Page,
                    request.PageSize,
                    totalItems,
                    "Applications retrieved successfully."
                );
            }
            catch (Exception ex)
            {
                return PagedResponse<IEnumerable<ApplicationResponse>>.Error($"Unexpected error: {ex.Message}");
            }
        }
        public async Task<BaseResponse<ApplicationResponse>> RejectApplicationAsync(Guid userId, Guid applicationId, RejectApplicationRequest request)
        {
            try
            {
                var selectedManager = await _unit.ManagerLanguages.Query()
                    .Include(s => s.User)
                    .Where(s => s.UserId == userId)
                    .FirstOrDefaultAsync();

                if (selectedManager == null)
                    return BaseResponse<ApplicationResponse>.Fail(errors: new object(), "Access denied.", 403);

                var existingApp = await _unit.TeacherApplications.Query()
                    .Include(a => a.Language)
                    .Include(a => a.User)
                    .Include(a => a.Certificates)
                        .ThenInclude(c => c.CertificateType)
                    .Where(a => a.ApplicationID == applicationId &&
                                a.Language.LanguageID == selectedManager.LanguageId)
                    .FirstOrDefaultAsync();

                if (existingApp == null)
                    return BaseResponse<ApplicationResponse>.Fail("Application not found or you are not allowed to reject this application.");

                if (existingApp.Status != ApplicationStatus.Pending)
                    return BaseResponse<ApplicationResponse>.Fail("Only pending applications can be rejected.");

                existingApp.Status = ApplicationStatus.Rejected;
                existingApp.RejectionReason = request.Reason;
                existingApp.ReviewedAt = TimeHelper.GetVietnamTime();
                existingApp.ReviewedBy = selectedManager.ManagerId;

                await _unit.SaveChangesAsync();

                BackgroundJob.Enqueue(() =>
                    _email.SendTeacherApplicationRejectedAsync(existingApp.User.Email, existingApp.User.UserName, existingApp.RejectionReason)
                );

                var response = new ApplicationResponse
                {
                    ApplicationID = existingApp.ApplicationID,
                    Language = existingApp.Language?.LanguageName,
                    FullName = existingApp.FullName,
                    DateOfBirth = existingApp.BirthDate.ToString("dd/MM/yyyy"),
                    Bio = existingApp.Bio,
                    Avatar = existingApp.Avatar,
                    Email = existingApp.Email,
                    PhoneNumber = existingApp.PhoneNumber,
                    MeetingUrl = existingApp.MeetingUrl,
                    TeachingExperience = existingApp.TeachingExperience,
                    Status = existingApp.Status.ToString(),
                    SubmittedAt = existingApp.SubmittedAt.ToString("dd/MM/yyyy HH:mm"),
                    ReviewedAt = existingApp.ReviewedAt.ToString("dd/MM/yyyy HH:mm"),
                    RejectionReason = existingApp.RejectionReason,
                    ProficiencyCode = existingApp.ProficiencyCode,
                    Submitter = existingApp.User == null ? null : new UserResponse
                    {
                        UserId = existingApp.User.UserID,
                        FullName = existingApp.User.FullName ?? existingApp.FullName,
                        Email = existingApp.User.Email,
                        PhoneNumber = existingApp.PhoneNumber
                    },
                    Reviewer = selectedManager.User == null ? null : new UserResponse
                    {
                        UserId = selectedManager.User.UserID,
                        FullName = selectedManager.User.FullName ?? selectedManager.User.UserName,
                        Email = selectedManager.User.Email
                    },
                    Certificates = existingApp.Certificates.Select(cert => new ApplicationCertTypeResponse
                    {
                        Id = cert.ApplicationCertTypeId,
                        CertificateImageUrl = cert.CertificateImageUrl,
                        CertificateName = cert.CertificateType?.Name
                    }).ToList()
                };

                return BaseResponse<ApplicationResponse>.Success(response, "Application rejected successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<ApplicationResponse>.Error($"Unexpected error: {ex.Message}");
            }
        }
        private static int GetProficiencyOrder(string langCode, string proficiencyCode)
        {
            if (string.IsNullOrWhiteSpace(langCode) || string.IsNullOrWhiteSpace(proficiencyCode))
                throw new ArgumentException("Language code and proficiency code are required.");

            langCode = langCode.ToLower().Trim();
            proficiencyCode = proficiencyCode.ToUpper().Trim();

            if (langCode == "en")
            {
                var map = new Dictionary<string, int>
        {
            { "A1", 1 }, { "A2", 2 },
            { "B1", 3 }, { "B2", 4 },
            { "C1", 5 }, { "C2", 6 }
        };
                if (map.TryGetValue(proficiencyCode, out int order))
                    return order;
                throw new ArgumentException($"Invalid English proficiency code: {proficiencyCode}. Allowed: A1–C2.");
            }

            if (langCode == "ja")
            {
                var map = new Dictionary<string, int>
        {
            { "N5", 1 }, { "N4", 2 },
            { "N3", 3 }, { "N2", 4 },
            { "N1", 5 }
        };
                if (map.TryGetValue(proficiencyCode, out int order))
                    return order;
                throw new ArgumentException($"Invalid Japanese proficiency code: {proficiencyCode}. Allowed: N5–N1.");
            }

            if (langCode == "zh")
            {
                var map = new Dictionary<string, int>
        {
            { "HSK1", 1 }, { "HSK2", 2 },
            { "HSK3", 3 }, { "HSK4", 4 },
            { "HSK5", 5 }, { "HSK6", 6 }
        };
                if (map.TryGetValue(proficiencyCode, out int order))
                    return order;
                throw new ArgumentException($"Invalid Chinese proficiency code: {proficiencyCode}. Allowed: HSK1–HSK6.");
            }

            throw new ArgumentException($"Unsupported language code: {langCode}. Allowed: en, ja, zh.");
        }
        public Task<BaseResponse<ApplicationResponse>> UpdateApplicationAsync(Guid userId, ApplicationUpdateRequest applicationRequest)
        {
            throw new NotImplementedException();
        }
        public async Task<BaseResponse<ApplicationResponse>> GetApplicationByIdAsync(Guid applicationId)
        {
            try
            {
                var existingApp = await _unit.TeacherApplications.Query()
                    .Include(a => a.Language)
                    .Include(a => a.User)
                    .Include(a => a.Certificates)
                        .ThenInclude(c => c.CertificateType)
                    .Where(a => a.ApplicationID == applicationId)
                    .FirstOrDefaultAsync();
                if (existingApp == null)
                {
                    return BaseResponse<ApplicationResponse>.Fail(null, "Application not found.", 404);
                }
                User selectedManager = null;
                if (existingApp.ReviewedBy != null)
                {
                    var managerLang = await _unit.ManagerLanguages.Query()
                        .Include(s => s.User)
                        .Where(s => s.ManagerId == existingApp.ReviewedBy)
                        .FirstOrDefaultAsync();
                    selectedManager = managerLang?.User;
                }
                var response = new ApplicationResponse
                {
                    ApplicationID = existingApp.ApplicationID,
                    Language = existingApp.Language?.LanguageName,
                    FullName = existingApp.FullName,
                    DateOfBirth = existingApp.BirthDate.ToString("dd/MM/yyyy"),
                    Bio = existingApp.Bio,
                    Avatar = existingApp.Avatar,
                    Email = existingApp.Email,
                    PhoneNumber = existingApp.PhoneNumber,
                    MeetingUrl = existingApp.MeetingUrl,
                    TeachingExperience = existingApp.TeachingExperience,
                    Status = existingApp.Status.ToString(),
                    SubmittedAt = existingApp.SubmittedAt.ToString("dd/MM/yyyy HH:mm"),
                    ReviewedAt = existingApp.ReviewedAt.ToString("dd/MM/yyyy HH:mm"),
                    RejectionReason = existingApp.RejectionReason,
                    ProficiencyCode = existingApp.ProficiencyCode,
                    Submitter = existingApp.User == null ? null : new UserResponse
                    {
                        UserId = existingApp.User.UserID,
                        FullName = existingApp.User.FullName ?? existingApp.FullName,
                        Email = existingApp.User.Email,
                        PhoneNumber = existingApp.PhoneNumber
                    },
                    Reviewer = selectedManager == null ? null : new UserResponse
                    {
                        UserId = selectedManager.UserID,
                        FullName = selectedManager.FullName ?? selectedManager.UserName,
                        Email = selectedManager.Email
                    },
                    Certificates = existingApp.Certificates.Select(cert => new ApplicationCertTypeResponse
                    {
                        Id = cert.ApplicationCertTypeId,
                        CertificateImageUrl = cert.CertificateImageUrl,
                        CertificateName = cert.CertificateType?.Name
                    }).ToList()
                };

                return BaseResponse<ApplicationResponse>.Success(response, "Application retrieved successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<ApplicationResponse>.Error($"Unexpected error: {ex.Message}");
            }
        }
    }
}
