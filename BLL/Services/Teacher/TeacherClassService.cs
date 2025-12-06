using BLL.IServices.Teacher;
using BLL.IServices.FirebaseService;
using BLL.IServices.Auth;
using Common.DTO.Learner;
using Common.DTO.Teacher;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace BLL.Services.Teacher
{
    public class TeacherClassService : ITeacherClassService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TeacherClassService> _logger;
        private readonly IFirebaseNotificationService _firebaseNotificationService;
        private readonly IEmailService _emailService;

        public TeacherClassService(
            IUnitOfWork unitOfWork, 
            ILogger<TeacherClassService> logger,
            IFirebaseNotificationService firebaseNotificationService,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _firebaseNotificationService = firebaseNotificationService;
            _emailService = emailService;
        }


        /// <summary>
        /// Hủy lớp học với logic kiểm tra 3 ngày
        /// - Nếu > 3 ngày: Hủy trực tiếp
        /// - Nếu ≤ 3 ngày: Throw exception yêu cầu dùng RequestCancelClassAsync
        /// </summary>
        public async Task<bool> CancelClassAsync(Guid teacherId, Guid classId, string reason)
        {
            try
            {
                var teacherClass = await _unitOfWork.TeacherClasses.GetByIdAsync(classId);

                if (teacherClass == null)
                    throw new KeyNotFoundException("Lớp học không tồn tại");

                if (teacherClass.TeacherID != teacherId)
                    throw new UnauthorizedAccessException("Bạn không có quyền thao tác với lớp học này");

                // Chỉ cho phép hủy lớp chưa hoàn thành
                if (teacherClass.Status == ClassStatus.Completed_Paid ||
                    teacherClass.Status == ClassStatus.Completed_PendingPayout)
                    throw new InvalidOperationException("Không thể hủy lớp học đã hoàn thành");

                // Kiểm tra lớp đã bắt đầu chưa
                var now = DateTime.UtcNow;
                if (teacherClass.StartDateTime <= now)
                    throw new InvalidOperationException("Không thể hủy lớp học đã bắt đầu");

                // ============================================
                // KIỂM TRA QUY TẮC 7 NGÀY (168 GIỜ)
                // ============================================
                var hoursUntilStart = (teacherClass.StartDateTime - now).TotalHours;

                if (hoursUntilStart <= 168) // 7 ngày = 168 giờ
                {
                    throw new InvalidOperationException(
                        $"Không thể hủy lớp trong vòng 7 ngày trước khi bắt đầu. " +
                        $"Vui lòng gửi yêu cầu hủy lớp để Manager xem xét bằng cách sử dụng chức năng 'Yêu cầu hủy lớp'."
                    );
                }

                // Nếu > 7 ngày → Cho phép hủy trực tiếp
                await ExecuteCancellationAsync(classId, reason);

                _logger.LogInformation("✅ Teacher {TeacherId} cancelled class {ClassId} (>7 days before start)",
                    teacherId, classId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cancelling class {ClassId}", classId);
                throw;
            }
        }

        /// <summary>
        /// Giáo viên gửi yêu cầu hủy lớp (dành cho lớp < 7 ngày)
        /// Returns: ID của yêu cầu hủy lớp đã tạo
        /// </summary>
        public async Task<Guid> RequestCancelClassAsync(Guid teacherId, Guid classId, string reason)
        {
            try
            {
                var teacherClass = await _unitOfWork.TeacherClasses.GetByIdAsync(classId);

                if (teacherClass == null)
                    throw new KeyNotFoundException("Lớp học không tồn tại");

                if (teacherClass.TeacherID != teacherId)
                    throw new UnauthorizedAccessException("Bạn không có quyền thao tác với lớp học này");

                var now = DateTime.UtcNow;
                if (teacherClass.StartDateTime <= now)
                    throw new InvalidOperationException("Không thể hủy lớp học đã bắt đầu");

                // Kiểm tra xem đã có yêu cầu pending chưa
                var hasPendingRequest = await _unitOfWork.ClassCancellationRequests.HasPendingRequestAsync(classId);
                if (hasPendingRequest)
                    throw new InvalidOperationException("Đã có yêu cầu hủy lớp đang chờ xử lý");

                // Tạo yêu cầu hủy lớp
                var request = new ClassCancellationRequest
                {
                    CancellationRequestId = Guid.NewGuid(),
                    ClassId = classId,
                    TeacherId = teacherId,
                    Reason = reason,
                    Status = CancellationRequestStatus.Pending,
                    RequestedAt = now
                };

                await _unitOfWork.ClassCancellationRequests.CreateAsync(request);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("📋 Teacher {TeacherId} requested to cancel class {ClassId} (reason: {Reason})",
                    teacherId, classId, reason);

                // === GỬI THÔNG BÁO CHO MANAGER ===
                await SendNotificationToManagersAsync(teacherClass, teacherId, reason);

                return request.CancellationRequestId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating cancellation request for class {ClassId}", classId);
                throw;
            }
        }

        /// <summary>
        /// Helper: Gửi Web Push notification cho Manager(s) quản lý ngôn ngữ của lớp
        /// </summary>
        private async Task SendNotificationToManagersAsync(TeacherClass teacherClass, Guid teacherId, string reason)
        {
            try
            {
                // Lấy thông tin teacher
                var teacher = await _unitOfWork.Users.GetByIdAsync(teacherId);
                var teacherName = teacher?.FullName ?? teacher?.UserName ?? "Giáo viên";

                // Lấy danh sách Manager quản lý ngôn ngữ này
                var managers = await _unitOfWork.ManagerLanguages.GetQuery()
                    .Where(m => m.LanguageId == teacherClass.LanguageID)
                    .Select(m => m.User)
                    .ToListAsync();

                var managerTokens = managers
                    .Where(m => m != null && !string.IsNullOrEmpty(m.FcmToken))
                    .Select(m => m!.FcmToken!)
                    .ToList();

                if (managerTokens.Any())
                {
                    await _firebaseNotificationService.SendNewCancellationRequestToManagerAsync(
                        managerTokens,
                        teacherName,
                        teacherClass.Title ?? "Lớp học",
                        reason
                    );

                    _logger.LogInformation("[FCM-Web] ✅ Sent cancellation request notification to {Count} manager(s)",
                        managerTokens.Count);
                }
                else
                {
                    _logger.LogWarning("[FCM-Web] ⚠️ No managers with FCM token found for language {LanguageId}",
                        teacherClass.LanguageID);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FCM-Web] ❌ Failed to send notification to managers");
            }
        }

        /// <summary>
        /// Helper method: Thực hiện hủy lớp và tạo RefundRequest
        /// Method này được dùng bởi:
        /// 1. CancelClassAsync() - khi hủy trực tiếp (> 7 ngày)
        /// 2. ClassAdminService.ApproveCancellationRequestAsync() - khi Manager duyệt
        /// </summary>
        private async Task ExecuteCancellationAsync(Guid classId, string reason)
        {
            var teacherClass = await _unitOfWork.TeacherClasses.GetByIdAsync(classId);
            if (teacherClass == null) return;

            var enrollments = await _unitOfWork.ClassEnrollments.GetEnrollmentsByClassAsync(classId);
            var paidEnrollments = enrollments.Where(e => e.Status == EnrollmentStatus.Paid).ToList();

            // Tạo RefundRequest cho từng học viên
            if (paidEnrollments.Any())
            {
                foreach (var enrollment in paidEnrollments)
                {
                    var refundRequest = new RefundRequest
                    {
                        RefundRequestID = Guid.NewGuid(),
                        EnrollmentID = enrollment.EnrollmentID,
                        ClassID = classId,
                        StudentID = enrollment.StudentID,
                        RequestType = RefundRequestType.ClassCancelled_TeacherUnavailable,
                        Reason = reason ?? "Teacher cancelled the class",
                        RefundAmount = enrollment.AmountPaid,
                        Status = RefundRequestStatus.Draft, // ✨ DRAFT: Chưa điền STK

                        // Để trống - học viên sẽ cập nhật sau
                        BankName = string.Empty,
                        BankAccountNumber = string.Empty,
                        BankAccountHolderName = string.Empty,

                        RequestedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.RefundRequests.CreateAsync(refundRequest);

                    // Cập nhật trạng thái enrollment
                    enrollment.Status = EnrollmentStatus.PendingRefund;
                    enrollment.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.ClassEnrollments.UpdateAsync(enrollment);

                    // Gửi thông báo FCM cho học viên
                    if (enrollment.Student != null && !string.IsNullOrEmpty(enrollment.Student.FcmToken))
                    {
                        try
                        {
                            await _firebaseNotificationService.SendNotificationAsync(
                                enrollment.Student.FcmToken,
                                "Lớp học đã bị hủy ❌",
                                $"Lớp '{teacherClass.Title}' đã bị hủy. Vui lòng cập nhật thông tin ngân hàng để nhận hoàn tiền.",
                                new Dictionary<string, string>
                                {
                            { "type", "class_cancelled_refund_required" },
                            { "refundRequestId", refundRequest.RefundRequestID.ToString() },
                            { "classId", classId.ToString() },
                            { "className", teacherClass.Title ?? "Lớp học" }
                                }
                            );

                            _logger.LogInformation("[FCM] ✅ Sent refund notification to student {StudentId}", enrollment.StudentID);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[FCM] ❌ Failed to send notification to student {StudentId}", enrollment.StudentID);
                        }
                    }

                    // 📧 GỬI EMAIL CHO HỌC VIÊN
                    if (enrollment.Student != null && !string.IsNullOrEmpty(enrollment.Student.Email))
                    {
                        try
                        {
                            await _emailService.SendRefundRequestInstructionAsync(
                                enrollment.Student.Email,
                                enrollment.Student.UserName,
                                teacherClass.Title ?? "Lớp học",
                                teacherClass.StartDateTime,
                                reason
                            );

                            _logger.LogInformation("[EMAIL] ✅ Sent refund email to {Email}", enrollment.Student.Email);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[EMAIL] ❌ Failed to send email to {Email}", enrollment.Student.Email);
                        }
                    }
                }

                _logger.LogInformation("🔄 Created {Count} refund requests for cancelled class {ClassId}",
                    paidEnrollments.Count, classId);
            }

            // Cập nhật trạng thái lớp
            teacherClass.Status = ClassStatus.Cancelled;
            teacherClass.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.TeacherClasses.UpdateAsync(teacherClass);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("✅ Class {ClassId} cancelled successfully", classId);
        }

        public async Task<TeacherClassDto> CreateClassAsync(Guid teacherId, CreateClassDto createClassDto)
        {
            try
            {
                var teacher = await _unitOfWork.TeacherProfiles.FindAsync(t => t.UserId == teacherId);
                if (teacher == null)
                    throw new UnauthorizedAccessException("Chỉ giáo viên mới có thể tạo lớp học");

                // Choose assignment: prefer the one provided, else highest level assigned
                TeacherProgramAssignment? assignment = null;
                if (createClassDto.ProgramAssignmentId.HasValue)
                {
                    assignment = await _unitOfWork.TeacherProgramAssignments.Query()
                        .Include(a => a.Program)
                        .Include(a => a.Level)
                        .FirstOrDefaultAsync(a => a.ProgramAssignmentId == createClassDto.ProgramAssignmentId.Value
                                                  && a.TeacherId == teacher.TeacherId && a.Status);
                    if (assignment == null)
                        throw new InvalidOperationException("Assignment không hợp lệ cho giáo viên");
                }
                else
                {
                    assignment = await _unitOfWork.TeacherProgramAssignments.Query()
                        .Include(a => a.Program)
                        .Include(a => a.Level)
                        .Where(a => a.TeacherId == teacher.TeacherId && a.Status)
                        .OrderByDescending(a => a.Level.OrderIndex)
                        .FirstOrDefaultAsync();
                    if (assignment == null)
                        throw new InvalidOperationException("Giáo viên chưa được gán chương trình/level phù hợp");
                }

                var startDateTime = createClassDto.ClassDate.Date + createClassDto.StartTime;
                var endDateTime = startDateTime.AddMinutes(createClassDto.DurationMinutes);
                if (startDateTime <= DateTime.UtcNow.AddDays(7))
                    throw new InvalidOperationException($"Thời gian bắt đầu phải sau ít nhất 7 ngày");

                var teacherClass = new TeacherClass
                {
                    ClassID = Guid.NewGuid(),
                    TeacherID = teacherId,
                    LanguageID = teacher.LanguageId,
                    ProgramAssignmentId = assignment.ProgramAssignmentId,
                    ProgramId = assignment.ProgramId,
                    LevelId = assignment.LevelId,
                    Title = createClassDto.Title,
                    Description = createClassDto.Description,
                    StartDateTime = startDateTime,
                    EndDateTime = endDateTime,
                    PricePerStudent = createClassDto.PricePerStudent,
                    GoogleMeetLink = teacher.MeetingUrl, // take from TeacherProfile
                    Status = ClassStatus.Draft,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.TeacherClasses.CreateAsync(teacherClass);
                await _unitOfWork.SaveChangesAsync();

                var createdClass = await _unitOfWork.TeacherClasses.GetClassWithEnrollmentsAsync(teacherClass.ClassID);
                return MapToTeacherClassDto(createdClass ?? teacherClass);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating class for teacher {TeacherId}", teacherId);
                throw;
            }
        }

        public async Task<bool> PublishClassAsync(Guid teacherId, Guid classId)
        {
            try
            {
                var teacherClass = await _unitOfWork.TeacherClasses.GetByIdAsync(classId);

                if (teacherClass == null)
                {
                    throw new KeyNotFoundException("Lớp học không tồn tại");
                }

                if (teacherClass.TeacherID != teacherId)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền thao tác với lớp học này");
                }

                if (teacherClass.Status != ClassStatus.Draft && teacherClass.Status != ClassStatus.Scheduled)
                {
                    throw new InvalidOperationException("Chỉ có thể publish lớp học ở trạng thái Draft hoặc Scheduled");
                }

                // Validate class can be published
                if (teacherClass.StartDateTime <= DateTime.UtcNow.AddHours(2))
                {
                    throw new InvalidOperationException("Không thể publish lớp học bắt đầu trong vòng 2 giờ tới");
                }

                if (string.IsNullOrEmpty(teacherClass.GoogleMeetLink))
                {
                    throw new InvalidOperationException("Vui lòng thêm link Google Meet trước khi publish");
                }

                teacherClass.Status = ClassStatus.Published;
                teacherClass.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.TeacherClasses.UpdateAsync(teacherClass);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("🎯 Class {ClassId} published by teacher {TeacherId}", classId, teacherId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error publishing class {ClassId}", classId);
                throw;
            }
        }

        public async Task<TeacherClassDto> UpdateClassAsync(Guid teacherId, Guid classId, UpdateClassDto updateClassDto)
        {
            try
            {
                var teacherClass = await _unitOfWork.TeacherClasses.GetByIdAsync(classId);

                if (teacherClass == null)
                {
                    throw new KeyNotFoundException("Lớp học không tồn tại");
                }

                if (teacherClass.TeacherID != teacherId)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền thao tác với lớp học này");
                }

                // Only allow updates for Draft and Scheduled classes
                if (teacherClass.Status != ClassStatus.Draft && teacherClass.Status != ClassStatus.Scheduled)
                {
                    throw new InvalidOperationException("Chỉ có thể chỉnh sửa lớp học ở trạng thái Draft hoặc Scheduled");
                }

                // Check if class has enrollments - limit what can be changed
                var enrollmentCount = await _unitOfWork.ClassEnrollments.GetEnrollmentCountByClassAsync(classId);

                // Update properties if provided
                if (!string.IsNullOrEmpty(updateClassDto.Title))
                {
                    teacherClass.Title = updateClassDto.Title;
                }

                if (!string.IsNullOrEmpty(updateClassDto.Description))
                {
                    teacherClass.Description = updateClassDto.Description;
                }

                if (updateClassDto.StartDateTime.HasValue)
                {
                    if (updateClassDto.StartDateTime.Value <= DateTime.UtcNow)
                    {
                        throw new InvalidOperationException("Thời gian bắt đầu phải sau thời điểm hiện tại");
                    }
                    teacherClass.StartDateTime = updateClassDto.StartDateTime.Value;
                }

                if (updateClassDto.EndDateTime.HasValue)
                {
                    if (updateClassDto.EndDateTime.Value <= teacherClass.StartDateTime)
                    {
                        throw new InvalidOperationException("Thời gian kết thúc phải sau thời gian bắt đầu");
                    }
                    teacherClass.EndDateTime = updateClassDto.EndDateTime.Value;
                }

                // Only allow capacity/pricing changes if no enrollments yet
                if (enrollmentCount == 0)
                {
                    if (updateClassDto.PricePerStudent.HasValue)
                    {
                        teacherClass.PricePerStudent = updateClassDto.PricePerStudent.Value;
                    }
                }
                else
                {
                    // If there are enrollments, only warn about restricted changes
                    if (updateClassDto.MinStudents.HasValue || updateClassDto.PricePerStudent.HasValue)
                    {
                        _logger.LogWarning("⚠️ Teacher {TeacherId} attempted to change capacity/pricing for class {ClassId} with existing enrollments",
                            teacherId, classId);
                        throw new InvalidOperationException("Không thể thay đổi sức chứa hoặc giá khi đã có học sinh đăng ký");
                    }
                }

                if (!string.IsNullOrEmpty(updateClassDto.GoogleMeetLink))
                {
                    teacherClass.GoogleMeetLink = updateClassDto.GoogleMeetLink;
                }

                teacherClass.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.TeacherClasses.UpdateAsync(teacherClass);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("✏️ Teacher {TeacherId} updated class {ClassId}", teacherId, classId);

                // Get updated class with full info
                var updatedClass = await _unitOfWork.TeacherClasses.GetClassWithEnrollmentsAsync(classId);
                return MapToTeacherClassDto(updatedClass ?? teacherClass);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating class {ClassId} by teacher {TeacherId}", classId, teacherId);
                throw;
            }
        }

        public async Task<TeacherClassDto> GetClassDetailsAsync(Guid teacherId, Guid classId)
        {
            try
            {
                var teacherClass = await _unitOfWork.TeacherClasses.GetClassWithEnrollmentsAsync(classId);

                if (teacherClass == null)
                {
                    throw new KeyNotFoundException("Lớp học không tồn tại");
                }

                if (teacherClass.TeacherID != teacherId)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền xem lớp học này");
                }

                _logger.LogInformation("📖 Teacher {TeacherId} viewed class details {ClassId}", teacherId, classId);

                return MapToTeacherClassDto(teacherClass);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting class details {ClassId} for teacher {TeacherId}", classId, teacherId);
                throw;
            }
        }

        public async Task<List<ClassEnrollmentDto>> GetClassEnrollmentsAsync(Guid teacherId, Guid classId)
        {
            try
            {
                // Verify teacher owns the class
                var teacherClass = await _unitOfWork.TeacherClasses.GetByIdAsync(classId);
                if (teacherClass == null)
                {
                    throw new KeyNotFoundException("Lớp học không tồn tại");
                }

                if (teacherClass.TeacherID != teacherId)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền xem danh sách học sinh của lớp học này");
                }

                // Get enrollments
                var enrollments = await _unitOfWork.ClassEnrollments.GetEnrollmentsByClassAsync(classId);

                var enrollmentDtos = enrollments.Select(e => new ClassEnrollmentDto
                {
                    EnrollmentID = e.EnrollmentID,
                    ClassID = e.ClassID,
                    StudentID = e.StudentID,
                    StudentName = e.Student?.FullName ?? "Unknown Student",
                    StudentEmail = e.Student?.Email ?? "",
                    AmountPaid = e.AmountPaid,
                    PaymentTransactionId = e.PaymentTransactionId,
                    Status = e.Status.ToString(),
                    EnrolledAt = e.EnrolledAt,
                    UpdatedAt = e.UpdatedAt
                }).ToList();

                _logger.LogInformation("📋 Teacher {TeacherId} viewed {Count} enrollments for class {ClassId}",
                    teacherId, enrollmentDtos.Count, classId);

                return enrollmentDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting enrollments for class {ClassId} by teacher {TeacherId}", classId, teacherId);
                throw;
            }
        }

        public async Task<List<TeacherClassDto>> GetTeacherClassesAsync(Guid teacherId, ClassStatus? status = null)
        {
            try
            {
                List<TeacherClass> classes;

                if (status.HasValue)
                {
                    classes = await _unitOfWork.TeacherClasses.GetTeacherClassesByStatusAsync(teacherId, status.Value);
                }
                else
                {
                    classes = await _unitOfWork.TeacherClasses.GetTeacherClassesAsync(teacherId);
                }

                var classDtos = classes.Select(MapToTeacherClassDto).ToList();

                _logger.LogInformation("📚 Teacher {TeacherId} retrieved {Count} classes with status filter: {Status}",
                    teacherId, classDtos.Count, status?.ToString() ?? "All");

                return classDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting classes for teacher {TeacherId}", teacherId);
                throw;
            }
        }

        public async Task<List<TeacherAssignmentDto>> GetMyProgramAssignmentsAsync(Guid userId)
        {
            // userId is the AspNet Users table id; map to TeacherProfile first
            var teacher = await _unitOfWork.TeacherProfiles.FindAsync(t => t.UserId == userId);
            if (teacher == null) throw new UnauthorizedAccessException("Chỉ giáo viên mới có thể xem chương trình được gán");

            var list = await _unitOfWork.TeacherProgramAssignments.Query()
                .Include(a => a.Program).ThenInclude(p => p.Language)
                .Include(a => a.Level)
                .Where(a => a.TeacherId == teacher.TeacherId)
                .OrderBy(a => a.Program.Name).ThenBy(a => a.Level.OrderIndex)
                .Select(a => new TeacherAssignmentDto
                {
                    ProgramAssignmentId = a.ProgramAssignmentId,
                    ProgramId = a.ProgramId,
                    ProgramName = a.Program.Name,
                    LevelId = a.LevelId,
                    LevelName = a.Level.Name,
                    OrderIndex = a.Level.OrderIndex,
                    LanguageName = a.Program.Language.LanguageName,
                    LanguageCode = a.Program.Language.LanguageCode,
                    Active = a.Status
                }).ToListAsync();

            return list;
        }

        // Helper method
        private TeacherClassDto MapToTeacherClassDto(TeacherClass teacherClass)
        {
            return new TeacherClassDto
            {
                ClassID = teacherClass.ClassID,
                Title = teacherClass.Title,
                Description = teacherClass.Description,
                LanguageID = teacherClass.LanguageID,
                LanguageName = teacherClass.Language?.LanguageName,
                StartDateTime = teacherClass.StartDateTime,
                EndDateTime = teacherClass.EndDateTime,
                Capacity = teacherClass.Capacity,
                PricePerStudent = teacherClass.PricePerStudent,
                GoogleMeetLink = teacherClass.GoogleMeetLink,
                Status = teacherClass.Status.ToString(),
                CurrentEnrollments = teacherClass.Enrollments?.Count(e => e.Status == EnrollmentStatus.Paid) ?? 0,
                CreatedAt = teacherClass.CreatedAt,
                UpdatedAt = teacherClass.UpdatedAt
            };
        }
    }
}
