using BLL.IServices.Application;
using BLL.IServices.Auth;
using BLL.IServices.Upload;
using Common.DTO.ApiResponse;
using Common.DTO.Application.Request;
using Common.DTO.Application.Response;
using Common.DTO.Certificate.Response;
using Common.DTO.Language.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.Upload;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
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

        public async Task<BaseResponse<ApplicationResponse>> ApproveApplicationAsync(Guid staffId, Guid appplicationId)
        {
            var selectedStaff = await _unit.StaffLanguages.Query()
                .Where(s => s.UserId == staffId)
                .FirstOrDefaultAsync();

            if (selectedStaff == null)
            {
                return BaseResponse<ApplicationResponse>.Fail("Staff does not exist.");
            }

            var existingApp = await _unit.TeacherApplications.Query()
                .Include(a => a.Language)
                .Include(a => a.User)
                .Include(a => a.Certificates)
                    .ThenInclude(c => c.CertificateType)
                .Where(a => a.ApplicationID == appplicationId && a.Language.LanguageID == selectedStaff.LanguageId)
                .FirstOrDefaultAsync();

            if (existingApp == null)
            {
                return BaseResponse<ApplicationResponse>.Fail("Application not found or you are not allowed to approve this application.");
            }

            if (existingApp.Status != ApplicationStatus.Pending)
            {
                return BaseResponse<ApplicationResponse>.Fail("Only pending applications can be approved.");
            }

            existingApp.Status = ApplicationStatus.Approved;
            existingApp.ReviewedAt = TimeHelper.GetVietnamTime();
            existingApp.ReviewedBy = selectedStaff.StaffLanguageId;
            existingApp.ReviewedAt = TimeHelper.GetVietnamTime();
            await _unit.TeacherApplications.UpdateAsync(existingApp);
            await _unit.SaveChangesAsync();

            var role = await _unit.Roles.Query().Where(r => r.Name == "Teacher").FirstOrDefaultAsync();

            var newUserRole = new UserRole
            {
                UserRoleID = Guid.NewGuid(),
                RoleID = role.RoleID,
                UserID = existingApp.UserID,
                CreatedAt = TimeHelper.GetVietnamTime(),
                UpdatedAt = TimeHelper.GetVietnamTime(),
            };

            await _unit.UserRoles.CreateAsync(newUserRole);


            var teacherProfile = new TeacherProfile
            {
                TeacherProfileId = Guid.NewGuid(),
                UserId = existingApp.UserID,
                LanguageId = existingApp.LanguageID,
                FullName = existingApp.FullName,
                BirthDate = existingApp.BirthDate,
                Bio = existingApp.Bio,
                Avatar = existingApp.Avatar,
                Email = existingApp.Email,
                PhoneNumber = existingApp.PhoneNumber,
                MeetingUrl = existingApp.MeetingUrl,
                CreatedAt = TimeHelper.GetVietnamTime(),
                UpdatedAt = TimeHelper.GetVietnamTime(),
            };

            await _unit.TeacherProfiles.CreateAsync(teacherProfile);

            await _email.SendTeacherApplicationApprovedAsync(existingApp.Email, existingApp.User.UserName);

            var response = new ApplicationResponse
            {
                ApplicationID = existingApp.ApplicationID,
                UserID = existingApp.UserID,
                LanguageID = existingApp.LanguageID,
                FullName = existingApp.FullName,
                BirthDate = existingApp.BirthDate,
                Bio = existingApp.Bio,
                Avatar = existingApp.Avatar,
                Email = existingApp.Email,
                PhoneNumber = existingApp.PhoneNumber,
                MeetingUrl = existingApp.MeetingUrl,
                TeachingExperience = existingApp.TeachingExperience,
                Status = existingApp.Status.ToString(),
                SubmittedAt = existingApp.SubmittedAt,
                ReviewedAt = existingApp.ReviewedAt,
                RejectionReason = existingApp.RejectionReason,
                Language = existingApp.Language == null ? null : new LanguageResponse
                {
                    Id = existingApp.Language.LanguageID,
                    LangName = existingApp.Language.LanguageName,
                    LangCode = existingApp.Language.LanguageCode
                },
                User = existingApp.User == null ? null : new UserResponse
                {
                    UserId = existingApp.User.UserID,
                    UserName = existingApp.User.UserName,
                    Email = existingApp.User.Email
                },
                Certificates = existingApp.Certificates.Select(cert => new ApplicationCertTypeResponse
                {
                    ApplicationCertTypeId = cert.ApplicationCertTypeId,
                    CertificateTypeId = cert.CertificateTypeId,
                    CertificateImageUrl = cert.CertificateImageUrl,
                    CertificateType = cert.CertificateType == null ? null : new CertificateResponse
                    {
                        CertificateId = cert.CertificateType.CertificateTypeId,
                        Name = cert.CertificateType.Name,
                        Description = cert.CertificateType.Description
                    }
                }).ToList()
            };

            return BaseResponse<ApplicationResponse>.Success(response, "Application approved successfully.");
        }

        public async Task<BaseResponse<ApplicationResponse>> CreateApplicationAsync(Guid userId, ApplicationRequest applicationRequest)
        {
            var selectedUser = await _unit.Users.GetByIdAsync(userId);

            if (selectedUser == null)
            {
                return BaseResponse<ApplicationResponse>.Fail("User does not exist.");
            }

            // Check if user already submitted an application
            var existingApp = await _unit.TeacherApplications
                .FindAsync(a => a.UserID == userId &&
                               (a.Status == ApplicationStatus.Pending || a.Status == ApplicationStatus.Approved));

            if (existingApp != null)
            {
                return BaseResponse<ApplicationResponse>.Fail("You have already submitted an application.");
            }

            var selectedLang = await _unit.Languages.FindByLanguageCodeAsync(applicationRequest.LangCode);

            if (selectedLang == null)
            {
                return BaseResponse<ApplicationResponse>.Fail("Language does not exist.");
            }

            var certificateTypeIds = applicationRequest.CertificateTypeIds
                .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(x => x.Trim())
                .ToArray();

            if (applicationRequest.CertificateImages.Length != certificateTypeIds.Length)
            {
                return BaseResponse<ApplicationResponse>.Fail("CertificateImages count must match CertificateTypeIds count.");
            }

            foreach (var idStr in certificateTypeIds)
            {
                if (!Guid.TryParse(idStr, out var certId))
                    return BaseResponse<ApplicationResponse>.Fail($"Invalid CertificateTypeId: {idStr}");

                var certType = await _unit.CertificateTypes.GetByIdAsync(certId);
                if (certType == null)
                    return BaseResponse<ApplicationResponse>.Fail($"CertificateTypeId {idStr} does not exist.");
            }

            var avatarUpload = await _cloudinary.UploadImageAsync(applicationRequest.Avatar, "avatars");
            if (avatarUpload == null)
                return BaseResponse<ApplicationResponse>.Fail("Avatar upload failed.");

            var teacherApp = new TeacherApplication
            {
                ApplicationID = Guid.NewGuid(),
                UserID = userId,
                LanguageID = selectedLang.LanguageID,
                FullName = applicationRequest.FullName,
                BirthDate = applicationRequest.BirthDate,
                Bio = applicationRequest.Bio,
                Avatar = avatarUpload.Url,
                Email = applicationRequest.Email,
                PhoneNumber = applicationRequest.PhoneNumber,
                MeetingUrl = applicationRequest.MeetingUrl,
                TeachingExperience = applicationRequest.TeachingExperience,
                Status = ApplicationStatus.Pending,
                SubmittedAt = TimeHelper.GetVietnamTime()
            };

            await _unit.TeacherApplications.CreateAsync(teacherApp);


            var certificateList = new List<ApplicationCertType>();
            for (int i = 0; i < applicationRequest.CertificateImages.Length; i++)
            {
                var certFile = applicationRequest.CertificateImages[i];
                var certId = Guid.Parse(certificateTypeIds[i]);

                UploadResultDto uploadResult;
                if (certFile.ContentType.Contains("image"))
                    uploadResult = await _cloudinary.UploadImageAsync(certFile, "certificates");
                else
                    uploadResult = await _cloudinary.UploadDocumentAsync(certFile, "certificates");

                if (uploadResult == null)
                    return BaseResponse<ApplicationResponse>.Fail($"Upload failed for certificate index {i + 1}.");

                var certEntity = new ApplicationCertType
                {
                    ApplicationCertTypeId = Guid.NewGuid(),
                    ApplicationId = teacherApp.ApplicationID,
                    CertificateTypeId = certId,
                    CertificateImageUrl = uploadResult.Url,
                    CertificateImagePublicId = uploadResult.PublicId,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
                };

                certificateList.Add(certEntity);
            }

            await _unit.ApplicationCertTypes.AddRangeAsync(certificateList);
            await _unit.SaveChangesAsync();

            await _email.SendTeacherApplicationSubmittedAsync(applicationRequest.Email, selectedUser.UserName);

            var response = new ApplicationResponse
            {
                ApplicationID = teacherApp.ApplicationID,
                UserID = teacherApp.UserID,
                LanguageID = teacherApp.LanguageID,
                FullName = teacherApp.FullName,
                BirthDate = teacherApp.BirthDate,
                Bio = teacherApp.Bio,
                Avatar = teacherApp.Avatar,
                Email = teacherApp.Email,
                PhoneNumber = teacherApp.PhoneNumber,
                MeetingUrl = teacherApp.MeetingUrl,
                TeachingExperience = teacherApp.TeachingExperience,
                Status = teacherApp.Status.ToString(),
                SubmittedAt = teacherApp.SubmittedAt,
                Language = new LanguageResponse
                {
                    Id = selectedLang.LanguageID,
                    LangName = selectedLang.LanguageName,
                    LangCode = selectedLang.LanguageCode,
                },
                User = new UserResponse
                {
                    UserId = selectedUser.UserID,
                    UserName = selectedUser.UserName,
                    Email = selectedUser.Email,
                },
                Certificates = teacherApp.Certificates.Select(cert => new ApplicationCertTypeResponse
                {
                    ApplicationCertTypeId = cert.ApplicationCertTypeId,
                    CertificateTypeId = cert.CertificateTypeId,
                    CertificateImageUrl = cert.CertificateImageUrl,
                    CertificateType = cert.CertificateType != null ? new CertificateResponse
                    {
                        CertificateId = cert.CertificateType.CertificateTypeId,
                        Name = cert.CertificateType.Name,
                        Description = cert.CertificateType.Description
                    }
                    : null
                }).ToList()
            };

            return BaseResponse<ApplicationResponse>.Success(response, "Application created successfully.");
        }

        public async Task<PagedResponse<IEnumerable<ApplicationResponse>>> GetApplicationAsync(Guid staffId, PagingRequest request, string status)
        {
            var selectedStaff = await _unit.Users.Query()
                .Include(u => u.StaffLanguage)
                .Where(u => u.UserID == staffId)
                .FirstOrDefaultAsync();

            if (selectedStaff == null)
            {
                return new PagedResponse<IEnumerable<ApplicationResponse>>
                {
                    Code = 400,
                    Message = "Staff does not exist.",
                    Status = "fail"
                };
            }

            if (selectedStaff.StaffLanguage == null)
            {
                return new PagedResponse<IEnumerable<ApplicationResponse>>
                {
                    Code = 400,
                    Message = "Staff language is not assigned.",
                    Status = "fail"
                };
            }

            var parsedStatus = string.IsNullOrWhiteSpace(status) ? ApplicationStatus.Pending
                : Enum.TryParse<ApplicationStatus>(status, true, out var s) ? s : ApplicationStatus.Pending;

            var query = _unit.TeacherApplications.Query()
                .Include(a => a.Language)
                .Include(a => a.User)
                .Include(a => a.Certificates)
                    .ThenInclude(c => c.CertificateType)
                .Where(a => a.Language.LanguageID == selectedStaff.StaffLanguage.LanguageId && a.Status == parsedStatus)
                .OrderByDescending(a => a.SubmittedAt);

            var totalItems = await query.CountAsync();

            var applications = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

            var responses = applications.Select(teacherApp => new ApplicationResponse
            {
                ApplicationID = teacherApp.ApplicationID,
                UserID = teacherApp.UserID,
                LanguageID = teacherApp.LanguageID,
                FullName = teacherApp.FullName,
                BirthDate = teacherApp.BirthDate,
                Bio = teacherApp.Bio,
                Avatar = teacherApp.Avatar,
                Email = teacherApp.Email,
                PhoneNumber = teacherApp.PhoneNumber,
                TeachingExperience = teacherApp.TeachingExperience,
                Status = teacherApp.Status.ToString(),
                SubmittedAt = teacherApp.SubmittedAt,
                ReviewedAt = teacherApp.ReviewedAt,
                RejectionReason = teacherApp.RejectionReason,
                Language = teacherApp.Language == null ? null : new LanguageResponse
                {
                    Id = teacherApp.Language.LanguageID,
                    LangName = teacherApp.Language.LanguageName,
                    LangCode = teacherApp.Language.LanguageCode
                },
                User = teacherApp.User == null ? null : new UserResponse
                {
                    UserId = teacherApp.User.UserID,
                    UserName = teacherApp.User.UserName,
                    Email = teacherApp.User.Email
                },
                Certificates = teacherApp.Certificates.Select(cert => new ApplicationCertTypeResponse
                {
                    ApplicationCertTypeId = cert.ApplicationCertTypeId,
                    CertificateTypeId = cert.CertificateTypeId,
                    CertificateImageUrl = cert.CertificateImageUrl,
                    CertificateType = cert.CertificateType == null ? null : new CertificateResponse
                    {
                        CertificateId = cert.CertificateType.CertificateTypeId,
                        Name = cert.CertificateType.Name,
                        Description = cert.CertificateType.Description
                    }
                }).ToList()
            }).ToList();

            return PagedResponse<IEnumerable<ApplicationResponse>>.Success(
                responses,
                request.Page,
                request.PageSize,
                totalItems,
                "Fetched successfully"
            );

        }

        public async Task<BaseResponse<ApplicationResponse>> GetMyApplicationAsync(Guid userId)
        {
            var teacherApp = await _unit.TeacherApplications.GetByUserIdAsync(userId);

            if (teacherApp == null)
                return BaseResponse<ApplicationResponse>.Fail("No application found for this user.");

            var response = new ApplicationResponse
            {
                ApplicationID = teacherApp.ApplicationID,
                UserID = teacherApp.UserID,
                LanguageID = teacherApp.LanguageID,
                FullName = teacherApp.FullName,
                BirthDate = teacherApp.BirthDate,
                Bio = teacherApp.Bio,
                Avatar = teacherApp.Avatar,
                Email = teacherApp.Email,
                PhoneNumber = teacherApp.PhoneNumber,
                TeachingExperience = teacherApp.TeachingExperience,
                Status = teacherApp.Status.ToString(),
                SubmittedAt = teacherApp.SubmittedAt,
                ReviewedAt = teacherApp.ReviewedAt,
                RejectionReason = teacherApp.RejectionReason,
                Language = teacherApp.Language == null ? null : new LanguageResponse
                {
                    Id = teacherApp.Language.LanguageID,
                    LangName = teacherApp.Language.LanguageName,
                    LangCode = teacherApp.Language.LanguageCode
                },
                User = teacherApp.User == null ? null : new UserResponse
                {
                    UserId = teacherApp.User.UserID,
                    UserName = teacherApp.User.UserName,
                    Email = teacherApp.User.Email
                },
                Certificates = teacherApp.Certificates.Select(cert => new ApplicationCertTypeResponse
                {
                    ApplicationCertTypeId = cert.ApplicationCertTypeId,
                    CertificateTypeId = cert.CertificateTypeId,
                    CertificateImageUrl = cert.CertificateImageUrl,
                    CertificateType = cert.CertificateType == null ? null : new CertificateResponse
                    {
                        CertificateId = cert.CertificateType.CertificateTypeId,
                        Name = cert.CertificateType.Name,
                        Description = cert.CertificateType.Description
                    }
                }).ToList()
            };

            return BaseResponse<ApplicationResponse>.Success(response, "Application retrieved successfully.");
        }

        public async Task<BaseResponse<ApplicationResponse>> RejectApplicationAsync(Guid staffId, Guid appplicationId, RejectApplicationRequest request)
        {
            var selectedStaff = await _unit.StaffLanguages.Query()
                .Where(s => s.UserId == staffId)
                .FirstOrDefaultAsync();

            if (selectedStaff == null)
            {
                return BaseResponse<ApplicationResponse>.Fail("Staff does not exist.");
            }

            var existingApp = await _unit.TeacherApplications.Query()
                .Include(a => a.Language)
                .Include(a => a.User)
                .Include(a => a.Certificates)
                    .ThenInclude(c => c.CertificateType)
                .Where(a => a.ApplicationID == appplicationId && a.Language.LanguageID == selectedStaff.LanguageId)
                .FirstOrDefaultAsync();

            if (existingApp == null)
            {
                return BaseResponse<ApplicationResponse>.Fail("Application not found or you are not allowed to reject this application.");
            }

            if (existingApp.Status != ApplicationStatus.Pending)
            {
                return BaseResponse<ApplicationResponse>.Fail("Only pending applications can be rejected.");
            }

            existingApp.Status = ApplicationStatus.Rejected;
            existingApp.RejectionReason = request.Reason;
            existingApp.ReviewedAt = TimeHelper.GetVietnamTime();
            existingApp.ReviewedBy = selectedStaff.StaffLanguageId;

            await _unit.TeacherApplications.UpdateAsync(existingApp);
            await _unit.SaveChangesAsync();

            await _email.SendTeacherApplicationRejectedAsync(existingApp.Email, existingApp.User.UserName, existingApp.RejectionReason);

            var response = new ApplicationResponse
            {
                ApplicationID = existingApp.ApplicationID,
                UserID = existingApp.UserID,
                LanguageID = existingApp.LanguageID,
                FullName = existingApp.FullName,
                BirthDate = existingApp.BirthDate,
                Bio = existingApp.Bio,
                Avatar = existingApp.Avatar,
                Email = existingApp.Email,
                PhoneNumber = existingApp.PhoneNumber,
                MeetingUrl = existingApp.MeetingUrl,
                TeachingExperience = existingApp.TeachingExperience,
                Status = existingApp.Status.ToString(),
                SubmittedAt = existingApp.SubmittedAt,
                ReviewedAt = existingApp.ReviewedAt,
                RejectionReason = existingApp.RejectionReason,
                Language = existingApp.Language == null ? null : new LanguageResponse
                {
                    Id = existingApp.Language.LanguageID,
                    LangName = existingApp.Language.LanguageName,
                    LangCode = existingApp.Language.LanguageCode
                },
                User = existingApp.User == null ? null : new UserResponse
                {
                    UserId = existingApp.User.UserID,
                    UserName = existingApp.User.UserName,
                    Email = existingApp.User.Email
                },
                Certificates = existingApp.Certificates.Select(cert => new ApplicationCertTypeResponse
                {
                    ApplicationCertTypeId = cert.ApplicationCertTypeId,
                    CertificateTypeId = cert.CertificateTypeId,
                    CertificateImageUrl = cert.CertificateImageUrl,
                    CertificateType = cert.CertificateType == null ? null : new CertificateResponse
                    {
                        CertificateId = cert.CertificateType.CertificateTypeId,
                        Name = cert.CertificateType.Name,
                        Description = cert.CertificateType.Description
                    }
                }).ToList()
            };

            return BaseResponse<ApplicationResponse>.Success(response, "Application rejected successfully.");
        }

        public async Task<BaseResponse<ApplicationResponse>> UpdateApplicationAsync(Guid userId, ApplicationRequest applicationRequest)
        {
            var existingApp = await _unit.TeacherApplications
                .FindAsync(a => a.UserID == userId && a.Language.LanguageCode == applicationRequest.LangCode);

            if (existingApp == null)
                return BaseResponse<ApplicationResponse>.Fail("Application not found.");

            if (existingApp.Status != ApplicationStatus.Pending && existingApp.Status != ApplicationStatus.Rejected)
                return BaseResponse<ApplicationResponse>.Fail("Only pending or rejected applications can be updated.");

            var selectedLang = await _unit.Languages.FindByLanguageCodeAsync(applicationRequest.LangCode);
            if (selectedLang == null)
                return BaseResponse<ApplicationResponse>.Fail("Language does not exist.");


            var certificateTypeIds = applicationRequest.CertificateTypeIds
                .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(x => x.Trim())
                .ToArray();

            if (applicationRequest.CertificateImages.Length != certificateTypeIds.Length)
                return BaseResponse<ApplicationResponse>.Fail("CertificateImages count must match CertificateTypeIds count.");

            foreach (var idStr in certificateTypeIds)
            {
                if (!Guid.TryParse(idStr, out var certId))
                    return BaseResponse<ApplicationResponse>.Fail($"Invalid CertificateTypeId: {idStr}");

                var certType = await _unit.CertificateTypes.GetByIdAsync(certId);
                if (certType == null)
                    return BaseResponse<ApplicationResponse>.Fail($"CertificateTypeId {idStr} does not exist.");
            }

            // Update avatar if new one is provided
            if (applicationRequest.Avatar != null)
            {
                var avatarUpload = await _cloudinary.UploadImageAsync(applicationRequest.Avatar, "avatars");
                if (avatarUpload == null)
                    return BaseResponse<ApplicationResponse>.Fail("Avatar upload failed.");
                existingApp.Avatar = avatarUpload.Url;
            }

            // Update basic info
            existingApp.FullName = applicationRequest.FullName ?? existingApp.FullName;
            if (applicationRequest.BirthDate != default)
                existingApp.BirthDate = applicationRequest.BirthDate;
            existingApp.Bio = applicationRequest.Bio ?? existingApp.Bio;
            existingApp.Email = applicationRequest.Email ?? existingApp.Email;
            existingApp.PhoneNumber = applicationRequest.PhoneNumber ?? existingApp.PhoneNumber;
            existingApp.MeetingUrl = applicationRequest.MeetingUrl ?? existingApp.MeetingUrl;
            existingApp.TeachingExperience = applicationRequest.TeachingExperience ?? existingApp.TeachingExperience;
            existingApp.Status = ApplicationStatus.Pending; // Reset lại để staff review lại
            existingApp.SubmittedAt = TimeHelper.GetVietnamTime();

            // Remove old certificates
            var oldCerts = await _unit.ApplicationCertTypes.FindAllAsync(x => x.ApplicationId == existingApp.ApplicationID);

            foreach (var cert in oldCerts)
            {
                if (!string.IsNullOrEmpty(cert.CertificateImagePublicId))
                    await _cloudinary.DeleteFileAsync(cert.CertificateImagePublicId);
            }

            _unit.ApplicationCertTypes.DeleteRange(oldCerts);

            // Add new certificates
            var certificateList = new List<ApplicationCertType>();
            for (int i = 0; i < applicationRequest.CertificateImages.Length; i++)
            {
                var certFile = applicationRequest.CertificateImages[i];
                var certId = Guid.Parse(certificateTypeIds[i]);

                UploadResultDto uploadResult;
                if (certFile.ContentType.Contains("image"))
                    uploadResult = await _cloudinary.UploadImageAsync(certFile, "certificates");
                else
                    uploadResult = await _cloudinary.UploadDocumentAsync(certFile, "certificates");

                if (uploadResult == null)
                    return BaseResponse<ApplicationResponse>.Fail($"Upload failed for certificate index {i + 1}.");

                certificateList.Add(new ApplicationCertType
                {
                    ApplicationCertTypeId = Guid.NewGuid(),
                    ApplicationId = existingApp.ApplicationID,
                    CertificateTypeId = certId,
                    CertificateImageUrl = uploadResult.Url,
                    CertificateImagePublicId = uploadResult.PublicId,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
                });
            }

            await _unit.ApplicationCertTypes.AddRangeAsync(certificateList);
            await _unit.SaveChangesAsync();

            var response = new ApplicationResponse
            {
                ApplicationID = existingApp.ApplicationID,
                UserID = existingApp.UserID,
                LanguageID = existingApp.LanguageID,
                FullName = existingApp.FullName,
                BirthDate = existingApp.BirthDate,
                Bio = existingApp.Bio,
                Avatar = existingApp.Avatar,
                Email = existingApp.Email,
                PhoneNumber = existingApp.PhoneNumber,
                TeachingExperience = existingApp.TeachingExperience,
                Status = existingApp.Status.ToString(),
                SubmittedAt = existingApp.SubmittedAt,
                ReviewedAt = existingApp.ReviewedAt,
                RejectionReason = existingApp.RejectionReason,
                Language = existingApp.Language == null ? null : new LanguageResponse
                {
                    Id = existingApp.Language.LanguageID,
                    LangName = existingApp.Language.LanguageName,
                    LangCode = existingApp.Language.LanguageCode
                },
                User = existingApp.User == null ? null : new UserResponse
                {
                    UserId = existingApp.User.UserID,
                    UserName = existingApp.User.UserName,
                    Email = existingApp.User.Email
                },
                Certificates = existingApp.Certificates.Select(cert => new ApplicationCertTypeResponse
                {
                    ApplicationCertTypeId = cert.ApplicationCertTypeId,
                    CertificateTypeId = cert.CertificateTypeId,
                    CertificateImageUrl = cert.CertificateImageUrl,
                    CertificateType = cert.CertificateType == null ? null : new CertificateResponse
                    {
                        CertificateId = cert.CertificateType.CertificateTypeId,
                        Name = cert.CertificateType.Name,
                        Description = cert.CertificateType.Description
                    }
                }).ToList()
            };

            return BaseResponse<ApplicationResponse>.Success(response, "Application updated successfully.");
        }
    }
}
