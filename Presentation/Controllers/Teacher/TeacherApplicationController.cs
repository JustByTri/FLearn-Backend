using BLL.IServices.Teacher;
using BLL.IServices.Upload;
using Common.DTO.Staff;
using Common.DTO.Teacher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.Teacher
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeacherApplicationController : ControllerBase
    {
        private readonly ITeacherApplicationService _teacherApplicationService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly DAL.UnitOfWork.IUnitOfWork _unitOfWork;

        public TeacherApplicationController(
            ITeacherApplicationService teacherApplicationService,
            ICloudinaryService cloudinaryService,
            DAL.UnitOfWork.IUnitOfWork unitOfWork)
        {
            _teacherApplicationService = teacherApplicationService;
            _cloudinaryService = cloudinaryService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Nộp đơn ứng tuyển làm giáo viên với upload file
        /// </summary>
        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateApplication([FromForm] TeacherApplicationFormDto formDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = ModelState
                    });
                }

                if (formDto.CredentialFiles.Count != formDto.CredentialNames.Count ||
                    formDto.CredentialFiles.Count != formDto.CredentialTypes.Count)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Số lượng file, tên chứng chỉ và loại chứng chỉ phải khớp nhau"
                    });
                }

                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                if (!await _teacherApplicationService.CanUserApplyAsync(userId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bạn không thể nộp đơn ứng tuyển. Có thể bạn đã là giáo viên hoặc đã có đơn đang chờ xét duyệt."
                    });
                }

                var credentials = new List<CreateCredentialDto>();
                for (int i = 0; i < formDto.CredentialFiles.Count; i++)
                {
                    try
                    {
                        var file = formDto.CredentialFiles[i];
                        var credentialName = formDto.CredentialNames[i];
                        var credentialType = formDto.CredentialTypes[i];

                        var fileUrl = await _cloudinaryService.UploadCredentialAsync(file, userId, credentialName);

                        credentials.Add(new CreateCredentialDto
                        {
                            CredentialName = credentialName,
                            Type = (CredentialType)credentialType,
                            CredentialFileUrl = fileUrl
                        });
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Lỗi upload file '{formDto.CredentialNames[i]}': {ex.Message}"
                        });
                    }
                }

                var applicationDto = new CreateTeacherApplicationDto
                {
                    LanguageID = formDto.LanguageID, // Thêm LanguageID
                    Motivation = formDto.Motivation,
                    Credentials = credentials
                };

                var result = await _teacherApplicationService.CreateApplicationAsync(userId, applicationDto);

                return Ok(new
                {
                    success = true,
                    message = "Nộp đơn ứng tuyển thành công! Chúng tôi sẽ xem xét và phản hồi trong vòng 3-5 ngày làm việc.",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi trong quá trình nộp đơn ứng tuyển",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy đơn ứng tuyển của người dùng hiện tại
        /// </summary>
        [HttpGet("my-application")]
        [Authorize]
        public async Task<IActionResult> GetMyApplication()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var application = await _teacherApplicationService.GetApplicationByUserAsync(userId);

                if (application == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Bạn chưa có đơn ứng tuyển nào"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin đơn ứng tuyển thành công",
                    data = application
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy thông tin đơn ứng tuyển",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Kiểm tra xem có thể nộp đơn ứng tuyển không
        /// </summary>
        [HttpGet("can-apply")]
        [Authorize]
        public async Task<IActionResult> CanApply()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var canApply = await _teacherApplicationService.CanUserApplyAsync(userId);

                return Ok(new
                {
                    success = true,
                    data = new { canApply = canApply },
                    message = canApply ? "Bạn có thể nộp đơn ứng tuyển" : "Bạn không thể nộp đơn ứng tuyển"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi kiểm tra điều kiện ứng tuyển",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy tất cả đơn ứng tuyển (Admin only)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Policy = "StaffOnly")]
        public async Task<IActionResult> GetAllApplications()
        {
            try
            {
                var applications = await _teacherApplicationService.GetAllApplicationsAsync();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách đơn ứng tuyển thành công",
                    data = applications,
                    total = applications.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy danh sách đơn ứng tuyển",
                    error = ex.Message
                });
            }
        }
        /// <summary>
        /// Lấy đơn ứng tuyển theo ngôn ngữ được phân công (STAFF + ADMIN)
        /// </summary>
        [HttpGet("my-assignments")]
        [Authorize(Policy = "StaffOnly")]
        public async Task<IActionResult> GetMyAssignedApplications()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

                // ✅ Admin có thể xem tất cả đơn ứng tuyển
                if (userRoles.Contains("Admin"))
                {
                    var allApplications = await _teacherApplicationService.GetAllApplicationsAsync();
                    return Ok(new
                    {
                        success = true,
                        message = "Lấy danh sách đơn ứng tuyển thành công (Admin - All)",
                        data = allApplications.OrderByDescending(x => x.AppliedAt).ToList(),
                        total = allApplications.Count,
                        userRole = "Admin",
                        assignedLanguages = "All Languages"
                    });
                }

                // ✅ Staff chỉ xem đơn ứng tuyển của ngôn ngữ được phân công
                var userLanguages = await _unitOfWork.UserLearningLanguages.GetLanguagesByUserAsync(userId);
                if (!userLanguages.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bạn chưa được phân công quản lý ngôn ngữ nào. Vui lòng liên hệ Admin để được phân công."
                    });
                }

                var staffApplications = new List<TeacherApplicationDto>();
                var assignedLanguageNames = new List<string>();

                foreach (var userLanguage in userLanguages)
                {
                    // Lấy thông tin ngôn ngữ
                    var language = await _unitOfWork.Languages.GetByIdAsync(userLanguage.LanguageID);
                    if (language != null)
                    {
                        assignedLanguageNames.Add(language.LanguageName);
                    }

                    // Lấy đơn ứng tuyển theo ngôn ngữ
                    var applications = await _teacherApplicationService.GetApplicationsByLanguageAsync(userLanguage.LanguageID);
                    staffApplications.AddRange(applications);
                }

                return Ok(new
                {
                    success = true,
                    message = $"Lấy danh sách đơn ứng tuyển thành công cho {string.Join(", ", assignedLanguageNames)}",
                    data = staffApplications.OrderByDescending(x => x.AppliedAt).ToList(),
                    total = staffApplications.Count,
                    userRole = "Staff",
                    assignedLanguages = assignedLanguageNames
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy danh sách đơn ứng tuyển",
                    error = ex.Message
                });
            }
        }
        /// <summary>
        /// Lấy đơn ứng tuyển theo ngôn ngữ của staff
        /// </summary>
        [HttpGet("my-language")]
        [Authorize(Policy = "StaffOnly")]
        public async Task<IActionResult> GetApplicationsByMyLanguage()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Lấy ngôn ngữ được phân công cho staff
                var userLanguages = await _unitOfWork.UserLearningLanguages.GetLanguagesByUserAsync(userId);
                if (!userLanguages.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bạn chưa được phân công ngôn ngữ nào"
                    });
                }

                var allApplications = new List<TeacherApplicationDto>();
                foreach (var userLanguage in userLanguages)
                {
                    var applications = await _teacherApplicationService.GetApplicationsByLanguageAsync(userLanguage.LanguageID);
                    allApplications.AddRange(applications);
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách đơn ứng tuyển thành công",
                    data = allApplications.OrderByDescending(x => x.AppliedAt).ToList(),
                    total = allApplications.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy danh sách đơn ứng tuyển",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy đơn ứng tuyển đang chờ duyệt theo ngôn ngữ của staff
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Policy = "StaffOnly")]
        public async Task<IActionResult> GetPendingApplications()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

                List<TeacherApplicationDto> applications;

                if (userRoles.Contains("Staff"))
                {
                  
                    applications = await _teacherApplicationService.GetPendingApplicationsAsync();
                }
                else
                {
                   
                    var userLanguages = await _unitOfWork.UserLearningLanguages.GetLanguagesByUserAsync(userId);
                    if (!userLanguages.Any())
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Bạn chưa được phân công ngôn ngữ nào"
                        });
                    }

                    applications = new List<TeacherApplicationDto>();
                    foreach (var userLanguage in userLanguages)
                    {
                        var languageApplications = await _teacherApplicationService.GetPendingApplicationsByLanguageAsync(userLanguage.LanguageID);
                        applications.AddRange(languageApplications);
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách đơn chờ duyệt thành công",
                    data = applications.OrderByDescending(x => x.AppliedAt).ToList(),
                    total = applications.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy danh sách đơn chờ duyệt",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy chi tiết đơn ứng tuyển (Staff only)
        /// </summary>
        [HttpGet("{applicationId:guid}")]
        [Authorize(Policy = "StaffOnly")]
        public async Task<IActionResult> GetApplicationById(Guid applicationId)
        {
            try
            {
                var application = await _teacherApplicationService.GetApplicationByIdAsync(applicationId);

                if (application == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy đơn ứng tuyển"
                    });
                }

                // Kiểm tra quyền xem của staff
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

                if (!userRoles.Contains("Staff"))
                {
                    var userLanguages = await _unitOfWork.UserLearningLanguages.GetLanguagesByUserAsync(userId);
                    if (!userLanguages.Any(ul => ul.LanguageID == application.LanguageID))
                    {
                        return Forbid("Bạn chỉ có thể xem đơn ứng tuyển cho ngôn ngữ được phân công");
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy chi tiết đơn ứng tuyển thành công",
                    data = application
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy chi tiết đơn ứng tuyển",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Duyệt hoặc từ chối đơn ứng tuyển (Staff only)
        /// </summary>
        [HttpPost("review")]
        [Authorize(Policy = "StaffOnly")]
        public async Task<IActionResult> ReviewApplication([FromBody] ReviewApplicationDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = ModelState
                    });
                }

                var reviewerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _teacherApplicationService.ReviewApplicationAsync(reviewerId, dto);

                return Ok(new
                {
                    success = true,
                    message = dto.IsApproved ? "Duyệt đơn ứng tuyển thành công" : "Từ chối đơn ứng tuyển thành công"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi xem xét đơn ứng tuyển",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy danh sách ngôn ngữ có thể ứng tuyển
        /// </summary>
        [HttpGet("languages")]
        [Authorize]
        public async Task<IActionResult> GetAvailableLanguages()
        {
            try
            {
                var languages = await _unitOfWork.Languages.GetAllAsync();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách ngôn ngữ thành công",
                    data = languages.Select(l => new
                    {
                        languageId = l.LanguageID,
                        languageName = l.LanguageName,
                        languageCode = l.LanguageCode
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy danh sách ngôn ngữ",
                    error = ex.Message
                });
            }
        }
    }
}
