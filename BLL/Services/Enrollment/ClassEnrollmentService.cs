using BLL.IServices.Enrollment;
using BLL.IServices.Payment;
using BLL.IServices.FirebaseService;
using Common.DTO.Learner;
using Common.DTO.Payment;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BLL.Services.Enrollment
{
    public class ClassEnrollmentService : IClassEnrollmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ClassEnrollmentService> _logger;
        private readonly IPayOSService _payOSService;
        private readonly IFirebaseNotificationService _firebaseNotificationService;

        public ClassEnrollmentService(
            IUnitOfWork unitOfWork,
            ILogger<ClassEnrollmentService> logger,
            IPayOSService payOSService,
            IFirebaseNotificationService firebaseNotificationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _payOSService = payOSService;
            _firebaseNotificationService = firebaseNotificationService;
        }

        /// <summary>
        /// Bước 1: Tạo link thanh toán (chưa tạo enrollment)
        /// </summary>
        public async Task<PaymentResponseDto?> CreatePaymentLinkAsync(Guid studentId, Guid classId, User student)
        {
            try
            {
                var teacherClass = await _unitOfWork.TeacherClasses.GetByIdAsync(classId);
                if (teacherClass == null)
                    throw new KeyNotFoundException("Class not found");

                // Kiểm tra student đã enroll chưa
                var existingEnrollment = await _unitOfWork.ClassEnrollments
                    .GetEnrollmentByStudentAndClassAsync(studentId, classId);

                if (existingEnrollment != null && existingEnrollment.Status != EnrollmentStatus.Cancelled)
                    throw new InvalidOperationException("Student already enrolled in this class");

                // Kiểm tra lớp còn chỗ không
                var currentEnrollments = await _unitOfWork.ClassEnrollments.GetQuery()
                    .Where(ce => ce.ClassID == classId && ce.Status == EnrollmentStatus.Paid)
                    .CountAsync();

                if (currentEnrollments >= teacherClass.Capacity)
                    throw new InvalidOperationException("Class is full");

                // Tạo payment link
                var paymentDto = new CreatePaymentDto
                {
                    ClassID = classId,
                    StudentID = studentId,
                    Amount = teacherClass.PricePerStudent,
                    Description = $"Enrollment for class {teacherClass.Title}",
                    ItemName = teacherClass.Title,
                    BuyerName = student.FullName,
                    BuyerEmail = student.Email,
                    BuyerPhone =  null,
                    
                };

                var paymentResponse = await _payOSService.CreatePaymentLinkAsync(paymentDto);

                if (!paymentResponse.Success)
                {
                    _logger.LogError("Failed to create payment link for class {ClassId}", classId);
                    return null;
                }

                _logger.LogInformation(
                    "Payment link created for student {StudentId} in class {ClassId}. TransactionId: {TransactionId}",
                    studentId, classId, paymentResponse.TransactionId);

                return paymentResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment link for student {StudentId} in class {ClassId}",
                    studentId, classId);
                throw;
            }
        }

        /// <summary>
        /// Bước 2: Sau khi callback thanh toán thành công, tạo enrollment
        /// </summary>
        public async Task<bool> ConfirmEnrollmentAsync(Guid studentId, Guid classId, string transactionId)
        {
            try
            {
                // Kiểm tra class tồn tại
                var teacherClass = await _unitOfWork.TeacherClasses.GetByIdAsync(classId);
                if (teacherClass == null)
                    throw new KeyNotFoundException("Class not found");

                // Kiểm tra student đã enroll chưa
                var existingEnrollment = await _unitOfWork.ClassEnrollments
                    .GetEnrollmentByStudentAndClassAsync(studentId, classId);

                if (existingEnrollment != null && existingEnrollment.Status == EnrollmentStatus.Paid)
                    throw new InvalidOperationException("Student already enrolled in this class");

                // Nếu có pending enrollment cũ, xóa nó
                if (existingEnrollment != null && existingEnrollment.Status == EnrollmentStatus.Pending)
                {
                    await _unitOfWork.ClassEnrollments.DeleteAsync(existingEnrollment.EnrollmentID);
                }

                // Tạo enrollment mới với trạng thái Paid
                var enrollment = new ClassEnrollment
                {
                    EnrollmentID = Guid.NewGuid(),
                    ClassID = classId,
                    StudentID = studentId,
                    AmountPaid = teacherClass.PricePerStudent,
                    Status = EnrollmentStatus.Paid,
                    PaymentTransactionId = transactionId,
                    EnrolledAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.ClassEnrollments.CreateAsync(enrollment);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Enrollment confirmed for student {StudentId} in class {ClassId}. TransactionId: {TransactionId}",
                    studentId, classId, transactionId);

                // Lấy thông tin student và teacher
                var student = await _unitOfWork.Users.GetByIdAsync(studentId);
                var teacher = await _unitOfWork.Users.GetByIdAsync(teacherClass.TeacherID);

                // === GỬI THÔNG BÁO CHO STUDENT (Mobile) ===
                if (student != null && !string.IsNullOrEmpty(student.FcmToken))
                {
                    try
                    {
                        await _firebaseNotificationService.SendClassRegistrationSuccessNotificationAsync(
                            student.FcmToken,
                            teacherClass.Title ?? "Lớp học",
                            teacherClass.StartDateTime
                        );
                        _logger.LogInformation("[FCM] ✅ Sent enrollment success notification to student {StudentId}", studentId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[FCM] ❌ Failed to send notification to student {StudentId}", studentId);
                    }
                }

                // === GỬI THÔNG BÁO CHO TEACHER (Web Push) ===
                if (teacher != null && !string.IsNullOrEmpty(teacher.FcmToken))
                {
                    try
                    {
                        // Đếm số học viên hiện tại
                        var currentEnrollments = await _unitOfWork.ClassEnrollments.GetQuery()
                            .Where(ce => ce.ClassID == classId && ce.Status == EnrollmentStatus.Paid)
                            .CountAsync();

                        await _firebaseNotificationService.SendNewEnrollmentNotificationToTeacherAsync(
                            teacher.FcmToken,
                            student?.FullName ?? student?.UserName ?? "Học viên",
                            teacherClass.Title ?? "Lớp học",
                            currentEnrollments,
                            teacherClass.Capacity
                        );

                        _logger.LogInformation("[FCM-Web] ✅ Sent new enrollment notification to teacher {TeacherId}", teacherClass.TeacherID);

                        // Nếu lớp đã đủ người, gửi thông báo đặc biệt
                        if (currentEnrollments >= teacherClass.Capacity)
                        {
                            await _firebaseNotificationService.SendClassFullNotificationToTeacherAsync(
                                teacher.FcmToken,
                                teacherClass.Title ?? "Lớp học",
                                currentEnrollments
                            );
                            _logger.LogInformation("[FCM-Web] ✅ Sent class full notification to teacher {TeacherId}", teacherClass.TeacherID);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[FCM-Web] ❌ Failed to send notification to teacher {TeacherId}", teacherClass.TeacherID);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming enrollment for student {StudentId} in class {ClassId}",
                    studentId, classId);
                throw;
            }
        }

        public async Task<ClassEnrollmentResponseDto?> GetEnrollmentAsync(Guid studentId, Guid enrollmentId)
        {
            try
            {
                var enrollment = await _unitOfWork.ClassEnrollments.GetByIdAsync(enrollmentId);
                if (enrollment?.StudentID != studentId)
                    return null;

                return new ClassEnrollmentResponseDto
                {
                    EnrollmentID = enrollment.EnrollmentID,
                    ClassID = enrollment.ClassID,
                    ClassName = enrollment.Class?.Title ?? "Unknown Class",
                    AmountToPay = enrollment.AmountPaid,
                    PaymentTransactionId = enrollment.PaymentTransactionId,
                    Status = enrollment.Status.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrollment {EnrollmentId}", enrollmentId);
                return null;
            }
        }

        

        public async Task<ClassEnrollmentResponseDto?> CreateEnrollmentAsync(Guid studentId, Guid classId)
        {
            throw new NotImplementedException("Use CreatePaymentLinkAsync instead");
        }
        public async Task<(List<AvailableClassDto> Classes, int TotalCount)> GetAvailableClassesAsync(Guid? languageId = null, string? status = null, int page = 1, int pageSize = 10)
        {
            try
            {
                var totalCount = await _unitOfWork.TeacherClasses.GetAvailableClassesCountAsync(languageId);
                var classes = await _unitOfWork.TeacherClasses.GetAvailableClassesPaginatedAsync(languageId, page, pageSize);

                var classeDtos = classes.Select(tc => new AvailableClassDto
                {
                    ClassID = tc.ClassID,
                    Title = tc.Title,
                    Description = tc.Description,
                    LanguageID = tc.LanguageID,
                    LanguageName = tc.Language?.LanguageName ?? "Unknown",
                    // Lấy FullName từ TeacherProfile, fallback UserName
                    TeacherName = tc.Teacher?.TeacherProfile?.FullName 
                                  ?? tc.Teacher?.UserName 
                                  ?? "Unknown Teacher",
                    StartDateTime = tc.StartDateTime,
                    EndDateTime = tc.EndDateTime,
                  
                    Capacity = tc.Capacity,
                    PricePerStudent = tc.PricePerStudent,
                    Status = tc.Status.ToString(),
                    CurrentEnrollments = tc.Enrollments?.Count(e => e.Status == EnrollmentStatus.Paid) ?? 0,
                    AvailableSlots = tc.Capacity - (tc.Enrollments?.Count(e => e.Status == EnrollmentStatus.Paid) ?? 0),
                    CreatedAt = tc.CreatedAt,
                    IsEnrollmentOpen = tc.Status == ClassStatus.Published &&
                                      tc.StartDateTime > DateTime.UtcNow &&
                                      (tc.Enrollments?.Count(e => e.Status == EnrollmentStatus.Paid) ?? 0) < tc.Capacity
                }).ToList();

                _logger.LogInformation("Retrieved {Count} available classes for languageId: {LanguageId}",
                    classeDtos.Count, languageId);

                return (classeDtos, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available classes for languageId: {LanguageId}", languageId);
                throw;
            }
        }
        public async Task<(List<EnrolledClassDto> Classes, int TotalCount)> GetStudentEnrolledClassesAsync(Guid studentId, EnrollmentStatus? status = null, int page = 1, int pageSize = 10)
        {
            try
            {
                var totalCount = await _unitOfWork.ClassEnrollments.GetEnrollmentsCountByStudentAsync(studentId, status);
                var enrollments = await _unitOfWork.ClassEnrollments.GetEnrollmentsByStudentPaginatedAsync(studentId, status, page, pageSize);

                var enrolledClassDtos = enrollments.Select(e => new EnrolledClassDto
                {
                    EnrollmentID = e.EnrollmentID,
                    ClassID = e.ClassID,
                    Title = e.Class?.Title ?? "Unknown Class",
                    Description = e.Class?.Description ?? "",
                    LanguageID = e.Class?.LanguageID ?? Guid.Empty,
                    LanguageName = e.Class?.Language?.LanguageName ?? "Unknown",
                    // Lấy FullName từ TeacherProfile, fallback UserName
                    TeacherName = e.Class?.Teacher?.TeacherProfile?.FullName 
                                  ?? e.Class?.Teacher?.UserName 
                                  ?? "Unknown Teacher",
                    StartDateTime = e.Class?.StartDateTime ?? DateTime.MinValue,
                    EndDateTime = e.Class?.EndDateTime ?? DateTime.MinValue,
                    AmountPaid = e.AmountPaid,
                    PaymentTransactionId = e.PaymentTransactionId ?? "",
                    EnrollmentStatus = e.Status.ToString(),
                    ClassStatus = e.Class?.Status.ToString() ?? "Unknown",
                    EnrolledAt = e.EnrolledAt,
                    TotalEnrollments = e.Class?.Enrollments?.Count(en => en.Status == EnrollmentStatus.Paid) ?? 0,
                    Capacity = e.Class?.Capacity ?? 0,
                    GoogleMeetLink = e.Class?.GoogleMeetLink ?? "",
                    CanJoinClass = CanStudentJoinClass(e),
                    IsClassStarted = e.Class?.StartDateTime <= DateTime.UtcNow,
                    IsClassFinished = e.Class?.EndDateTime <= DateTime.UtcNow
                }).ToList();

                _logger.LogInformation("Retrieved {Count} enrolled classes for student {StudentId}", enrolledClassDtos.Count, studentId);

                return (enrolledClassDtos, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrolled classes for student {StudentId}", studentId);
                throw;
            }
        }

        /// <summary>
        /// Học viên tự hủy đăng ký lớp (trong vòng 3 ngày trước khi lớp bắt đầu)
        /// </summary>
        public async Task<bool> CancelEnrollmentAsync(Guid studentId, Guid enrollmentId, string? reason = null)
        {
            try
            {
                // 1. Lấy thông tin enrollment
                var enrollment = await _unitOfWork.ClassEnrollments.GetByIdAsync(enrollmentId);
                
                if (enrollment == null)
                    throw new KeyNotFoundException("Enrollment not found");

                if (enrollment.StudentID != studentId)
                    throw new UnauthorizedAccessException("You don't have permission to cancel this enrollment");

                // 2. Kiểm tra trạng thái enrollment
                if (enrollment.Status != EnrollmentStatus.Paid)
                    throw new InvalidOperationException($"Cannot cancel enrollment with status: {enrollment.Status}");

                // 3. Lấy thông tin lớp học
                var teacherClass = await _unitOfWork.TeacherClasses.GetByIdAsync(enrollment.ClassID);
                if (teacherClass == null)
                    throw new KeyNotFoundException("Class not found");

                var now = DateTime.UtcNow;

                // ============================================
                // KIỂM TRA QUY TẮC 3 NGÀY TRƯỚC KHI LỚP BẮT ĐẦU (72 GIỜ)
                // ============================================
                var hoursUntilClassStart = (teacherClass.StartDateTime - now).TotalHours;

                if (hoursUntilClassStart <= 72) // 3 ngày = 72 giờ
                {
                    throw new InvalidOperationException(
                        $"Không thể hủy đăng ký trong vòng 3 ngày trước khi lớp bắt đầu. " +
                        $"Lớp sẽ bắt đầu sau {(int)(hoursUntilClassStart / 24)} ngày."
                    );
                }

                // 4. Kiểm tra lớp đã bắt đầu chưa (double-check)
                if (teacherClass.StartDateTime <= now)
                {
                    throw new InvalidOperationException("Cannot cancel enrollment for a class that has already started");
                }

                // 5. Tạo RefundRequest
                var refundRequest = new DAL.Models.RefundRequest
                {
                    RefundRequestID = Guid.NewGuid(),
                    EnrollmentID = enrollment.EnrollmentID,
                    ClassID = enrollment.ClassID,
                    StudentID = studentId,
                    RequestType = DAL.Models.RefundRequestType.StudentPersonalReason,
                    Reason = reason ?? "Student cancelled enrollment more than 3 days before class starts",
                    RefundAmount = enrollment.AmountPaid,
                    Status = DAL.Models.RefundRequestStatus.Pending,
                    
                    // Để trống - học viên cần cập nhật sau
                    BankName = string.Empty,
                    BankAccountNumber = string.Empty,
                    BankAccountHolderName = string.Empty,
                    
                    RequestedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _unitOfWork.RefundRequests.CreateAsync(refundRequest);

                // 6. Cập nhật trạng thái enrollment
                enrollment.Status = EnrollmentStatus.PendingRefund;
                enrollment.UpdatedAt = now;
                await _unitOfWork.ClassEnrollments.UpdateAsync(enrollment);

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ Student {StudentId} cancelled enrollment {EnrollmentId} (>3 days before class starts). RefundRequest {RefundRequestId} created.",
                    studentId, enrollmentId, refundRequest.RefundRequestID);

                // 7. Gửi thông báo FCM
                var student = await _unitOfWork.Users.GetByIdAsync(studentId);
                if (student != null && !string.IsNullOrEmpty(student.FcmToken))
                {
                    try
                    {
                        await _firebaseNotificationService.SendNotificationAsync(
                            student.FcmToken,
                            "Hủy đăng ký thành công ✅",
                            $"Bạn đã hủy đăng ký lớp '{teacherClass.Title}'. Vui lòng cập nhật thông tin ngân hàng để nhận hoàn tiền.",
                            new Dictionary<string, string>
                            {
                                { "type", "enrollment_cancelled_refund_required" },
                                { "refundRequestId", refundRequest.RefundRequestID.ToString() },
                                { "enrollmentId", enrollmentId.ToString() },
                                { "className", teacherClass.Title ?? "Lớp học" }
                            }
                        );

                        _logger.LogInformation("[FCM] ✅ Sent cancellation notification to student {StudentId}", studentId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[FCM] ❌ Failed to send notification to student {StudentId}", studentId);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cancelling enrollment {EnrollmentId} for student {StudentId}",
                    enrollmentId, studentId);
                throw;
            }
        }

        private bool CanStudentJoinClass(ClassEnrollment enrollment)
        {
            if (enrollment.Status != EnrollmentStatus.Paid || enrollment.Class == null)
                return false;

            var now = DateTime.UtcNow;
            var classStartTime = enrollment.Class.StartDateTime;
            var classEndTime = enrollment.Class.EndDateTime;

            // Có thể join nếu lớp đã bắt đầu nhưng chưa kết thúc
            return now >= classStartTime && now <= classEndTime &&
                   (enrollment.Class.Status == ClassStatus.InProgress || enrollment.Class.Status == ClassStatus.Published);
        }
    }
}