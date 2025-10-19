using BLL.IServices.Enrollment;
using BLL.IServices.Payment;
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

        public ClassEnrollmentService(
            IUnitOfWork unitOfWork,
            ILogger<ClassEnrollmentService> logger,
            IPayOSService payOSService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _payOSService = payOSService;
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
                    TeacherName = tc.Teacher?.FullName ?? "Unknown Teacher",
                    StartDateTime = tc.StartDateTime,
                    EndDateTime = tc.EndDateTime,
                    MinStudents = tc.MinStudents,
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
                    TeacherName = e.Class?.Teacher?.FullName ?? "Unknown Teacher",
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