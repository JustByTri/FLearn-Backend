using BLL.IServices.Teacher;
using BLL.IServices.FirebaseService;
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

        public TeacherClassService(
            IUnitOfWork unitOfWork, 
            ILogger<TeacherClassService> logger,
            IFirebaseNotificationService firebaseNotificationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _firebaseNotificationService = firebaseNotificationService;
        }

        public async Task<bool> CancelClassAsync(Guid teacherId, Guid classId, string reason)
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

                // Only allow cancellation for certain statuses
                if (teacherClass.Status == ClassStatus.Completed_Paid ||
                    teacherClass.Status == ClassStatus.Completed_PendingPayout)
                {
                    throw new InvalidOperationException("Không thể hủy lớp học đã hoàn thành");
                }

                // Check if class has started
                if (teacherClass.StartDateTime <= DateTime.UtcNow)
                {
                    throw new InvalidOperationException("Không thể hủy lớp học đã bắt đầu");
                }

                // Get enrollments to handle refunds
                var enrollments = await _unitOfWork.ClassEnrollments.GetEnrollmentsByClassAsync(classId);
                var paidEnrollments = enrollments.Where(e => e.Status == EnrollmentStatus.Paid).ToList();

                // If there are paid enrollments, need to process refunds and send notifications
                if (paidEnrollments.Any())
                {
                    foreach (var enrollment in paidEnrollments)
                    {
                        enrollment.Status = EnrollmentStatus.Refunded;
                        enrollment.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.ClassEnrollments.UpdateAsync(enrollment);

                        // Gửi thông báo hủy lớp cho học viên
                        if (enrollment.Student != null && !string.IsNullOrEmpty(enrollment.Student.FcmToken))
                        {
                            try
                            {
                                await _firebaseNotificationService.SendClassCancellationNotificationAsync(
                                    enrollment.Student.FcmToken,
                                    teacherClass.Title ?? "Lớp học",
                                    reason
                                );
                                _logger.LogInformation($"[FCM] Sent cancellation notification to student {enrollment.StudentID}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"[FCM] Failed to send cancellation notification to student {enrollment.StudentID}");
                            }
                        }
                    }

                    _logger.LogInformation("🔄 Refunded {Count} enrollments for cancelled class {ClassId}",
                        paidEnrollments.Count, classId);
                }

                // Update class status
                teacherClass.Status = ClassStatus.Cancelled;
                teacherClass.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.TeacherClasses.UpdateAsync(teacherClass);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("❌ Class {ClassId} cancelled by teacher {TeacherId}. Reason: {Reason}",
                    classId, teacherId, reason);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cancelling class {ClassId} by teacher {TeacherId}", classId, teacherId);
                throw;
            }
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
