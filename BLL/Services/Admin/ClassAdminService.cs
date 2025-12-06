using BLL.IServices.Admin;
using BLL.IServices.FirebaseService;
using BLL.IServices.Auth;
using Common.DTO.Admin;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.Admin
{
    public class ClassAdminService : IClassAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ClassAdminService> _logger;
        private readonly IFirebaseNotificationService _firebaseNotificationService;
        private readonly IEmailService _emailService;

        public ClassAdminService(
            IUnitOfWork unitOfWork, 
            ILogger<ClassAdminService> logger,
            IFirebaseNotificationService firebaseNotificationService,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _firebaseNotificationService = firebaseNotificationService;
            _emailService = emailService;
        }

        /// <summary>
        /// Manager duyệt yêu cầu hủy lớp
        /// </summary>
        public async Task<bool> ApproveCancellationRequestAsync(Guid managerId, Guid requestId, string? note)
        {
            var request = await _unitOfWork.ClassCancellationRequests.GetByIdWithDetailsAsync(requestId);

            if (request == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu hủy lớp");

            if (request.Status != CancellationRequestStatus.Pending)
                throw new InvalidOperationException($"Yêu cầu đã được xử lý với trạng thái: {request.Status}");

            // Kiểm tra manager có quyền duyệt không (cùng ngôn ngữ)
            var manager = await _unitOfWork.ManagerLanguages.FindAsync(m => m.UserId == managerId);
            if (manager == null || manager.LanguageId != request.TeacherClass.LanguageID)
                throw new UnauthorizedAccessException("Bạn không có quyền duyệt yêu cầu này");

            // Cập nhật request
            request.Status = CancellationRequestStatus.Approved;
            request.ProcessedByManagerId = managerId;
            request.ManagerNote = note;
            request.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.ClassCancellationRequests.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            // Thực hiện hủy lớp
            await ExecuteCancellationLogic(request.ClassId, request.Reason);

            _logger.LogInformation("✅ Manager {ManagerId} approved cancellation request {RequestId}",
                managerId, requestId);

            // === GỬI THÔNG BÁO CHO TEACHER ===
            await SendCancellationResultToTeacherAsync(request, isApproved: true, note);

            return true;
        }

        /// <summary>
        /// Manager từ chối yêu cầu hủy lớp
        /// </summary>
        public async Task<bool> RejectCancellationRequestAsync(Guid managerId, Guid requestId, string reason)
        {
            var request = await _unitOfWork.ClassCancellationRequests.GetByIdWithDetailsAsync(requestId);

            if (request == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu hủy lớp");

            if (request.Status != CancellationRequestStatus.Pending)
                throw new InvalidOperationException($"Yêu cầu đã được xử lý với trạng thái: {request.Status}");

            var manager = await _unitOfWork.ManagerLanguages.FindAsync(m => m.UserId == managerId);
            if (manager == null || manager.LanguageId != request.TeacherClass.LanguageID)
                throw new UnauthorizedAccessException("Bạn không có quyền từ chối yêu cầu này");

            request.Status = CancellationRequestStatus.Rejected;
            request.ProcessedByManagerId = managerId;
            request.ManagerNote = reason;
            request.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.ClassCancellationRequests.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("❌ Manager {ManagerId} rejected cancellation request {RequestId}",
                managerId, requestId);

            // === GỬI THÔNG BÁO CHO TEACHER ===
            await SendCancellationResultToTeacherAsync(request, isApproved: false, reason);

            return true;
        }

        /// <summary>
        /// Helper: Gửi Web Push notification cho Teacher về kết quả duyệt/từ chối
        /// </summary>
        private async Task SendCancellationResultToTeacherAsync(
            ClassCancellationRequest request, 
            bool isApproved, 
            string? reason)
        {
            try
            {
                var teacher = await _unitOfWork.Users.GetByIdAsync(request.TeacherId);
                if (teacher == null || string.IsNullOrEmpty(teacher.FcmToken))
                {
                    _logger.LogWarning("[FCM-Web] ⚠️ Teacher {TeacherId} has no FCM token", request.TeacherId);
                    return;
                }

                await _firebaseNotificationService.SendCancellationRequestResultToTeacherAsync(
                    teacher.FcmToken,
                    request.TeacherClass?.Title ?? "Lớp học",
                    isApproved,
                    reason
                );

                _logger.LogInformation("[FCM-Web] ✅ Sent cancellation result notification to teacher {TeacherId}",
                    request.TeacherId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FCM-Web] ❌ Failed to send notification to teacher");
            }
        }

        /// <summary>
        /// Lấy danh sách yêu cầu hủy lớp đang chờ duyệt
        /// </summary>
        public async Task<IEnumerable<ClassCancellationRequestDto>> GetPendingCancellationRequestsAsync(Guid managerId)
        {
            var manager = await _unitOfWork.ManagerLanguages.FindAsync(m => m.UserId == managerId);
            if (manager == null)
                throw new UnauthorizedAccessException("Chỉ Manager mới có quyền xem danh sách yêu cầu");

            var requests = await _unitOfWork.ClassCancellationRequests
                .GetPendingRequestsByManagerLanguageAsync(manager.LanguageId);

            var result = new List<ClassCancellationRequestDto>();

            foreach (var request in requests)
            {
                // Đếm số học viên đã đăng ký
                var enrollments = await _unitOfWork.ClassEnrollments
                    .GetEnrollmentsByClassAsync(request.ClassId);

                var paidEnrollments = enrollments.Where(e => e.Status == EnrollmentStatus.Paid).ToList();

                var dto = new ClassCancellationRequestDto
                {
                    CancellationRequestId = request.CancellationRequestId,
                    ClassId = request.ClassId,
                    ClassName = request.TeacherClass?.Title ?? "Unknown",
                    ClassStartDateTime = request.TeacherClass?.StartDateTime ?? DateTime.MinValue,
                    TeacherId = request.TeacherId,
                    TeacherName = request.Teacher?.FullName ?? request.Teacher?.UserName ?? "Unknown",
                    TeacherEmail = request.Teacher?.Email ?? "",
                    Reason = request.Reason,
                    Status = request.Status.ToString(),
                    ManagerNote = request.ManagerNote,
                    ProcessedByManagerName = request.ProcessedByManager?.FullName,
                    RequestedAt = request.RequestedAt.ToString("dd-MM-yyyy HH:mm"),
                    ProcessedAt = request.ProcessedAt?.ToString("dd-MM-yyyy HH:mm"),
                    EnrolledStudentsCount = paidEnrollments.Count,
                    TotalRefundAmount = paidEnrollments.Sum(e => e.AmountPaid)
                };

                result.Add(dto);
            }

            return result;
        }

        /// <summary>
        /// Lấy chi tiết một yêu cầu hủy lớp
        /// </summary>
        public async Task<ClassCancellationRequestDto> GetCancellationRequestByIdAsync(Guid managerId, Guid requestId)
        {
            var manager = await _unitOfWork.ManagerLanguages.FindAsync(m => m.UserId == managerId);
            if (manager == null)
                throw new UnauthorizedAccessException("Chỉ Manager mới có quyền xem chi tiết yêu cầu");

            var request = await _unitOfWork.ClassCancellationRequests.GetByIdWithDetailsAsync(requestId);

            if (request == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu hủy lớp");

            if (request.TeacherClass.LanguageID != manager.LanguageId)
                throw new UnauthorizedAccessException("Bạn không có quyền xem yêu cầu này");

            var enrollments = await _unitOfWork.ClassEnrollments.GetEnrollmentsByClassAsync(request.ClassId);
            var paidEnrollments = enrollments.Where(e => e.Status == EnrollmentStatus.Paid).ToList();

            return new ClassCancellationRequestDto
            {
                CancellationRequestId = request.CancellationRequestId,
                ClassId = request.ClassId,
                ClassName = request.TeacherClass?.Title ?? "Unknown",
                ClassStartDateTime = request.TeacherClass?.StartDateTime ?? DateTime.MinValue,
                TeacherId = request.TeacherId,
                TeacherName = request.Teacher?.FullName ?? request.Teacher?.UserName ?? "Unknown",
                TeacherEmail = request.Teacher?.Email ?? "",
                Reason = request.Reason,
                Status = request.Status.ToString(),
                ManagerNote = request.ManagerNote,
                ProcessedByManagerName = request.ProcessedByManager?.FullName,
                RequestedAt = request.RequestedAt.ToString("dd-MM-yyyy HH:mm"),
                ProcessedAt = request.ProcessedAt?.ToString("dd-MM-yyyy HH:mm"),
                EnrolledStudentsCount = paidEnrollments.Count,
                TotalRefundAmount = paidEnrollments.Sum(e => e.AmountPaid)
            };
        }

        /// <summary>
        /// Logic hủy lớp và tạo RefundRequest
        /// </summary>
        private async Task ExecuteCancellationLogic(Guid classId, string reason)
        {
            var teacherClass = await _unitOfWork.TeacherClasses.GetByIdAsync(classId);
            if (teacherClass == null) return;

            var enrollments = await _unitOfWork.ClassEnrollments.GetEnrollmentsByClassAsync(classId);
            var paidEnrollments = enrollments.Where(e => e.Status == EnrollmentStatus.Paid).ToList();

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
                        Reason = reason ?? "Manager approved class cancellation",
                        RefundAmount = enrollment.AmountPaid,
                        Status = RefundRequestStatus.Draft,
                        BankName = string.Empty,
                        BankAccountNumber = string.Empty,
                        BankAccountHolderName = string.Empty,
                        RequestedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.RefundRequests.CreateAsync(refundRequest);

                    enrollment.Status = EnrollmentStatus.PendingRefund;
                    enrollment.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.ClassEnrollments.UpdateAsync(enrollment);

                    // Gửi FCM notification cho học viên
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
                                    { "classId", classId.ToString() }
                                }
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[FCM] ❌ Failed to send notification to student {StudentId}", enrollment.StudentID);
                        }
                    }

                    // Gửi email cho học viên
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
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[EMAIL] ❌ Failed to send email to {Email}", enrollment.Student.Email);
                        }
                    }
                }
            }

            teacherClass.Status = ClassStatus.Cancelled;
            teacherClass.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.TeacherClasses.UpdateAsync(teacherClass);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("✅ Class {ClassId} cancelled via Manager approval", classId);
        }

        public async Task<List<object>> GetAllDisputesAsync()
        {
            try
            {
                var disputes = await _unitOfWork.ClassDisputes.GetAllAsync();

                return disputes.Where(d => d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview)
                    .Select(d => new
                    {
                        DisputeID = d.DisputeID,
                        ClassID = d.ClassID,
                        StudentID = d.StudentID,
                        Reason = d.Reason,
                        Description = d.Description,
                        Status = d.Status.ToString(),
                        CreatedAt = d.CreatedAt,
                        ClassName = d.Class?.Title,
                        StudentName = d.Student?.FullName
                    }).Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all disputes");
                throw;
            }
        }

        public async Task<bool> ResolveDisputeAsync(Guid disputeId, ResolveDisputeDto dto)
        {
            try
            {
                var dispute = await _unitOfWork.ClassDisputes.GetByIdAsync(disputeId);
                if (dispute == null) return false;

                dispute.Status = dto.Resolution.ToLower() switch
                {
                    "refund" => DisputeStatus.Resolved_Refunded,
                    "partial" => DisputeStatus.Resolved_PartialRefund,
                    "refuse" => DisputeStatus.Resolved_Refused,
                    _ => DisputeStatus.Closed
                };

                dispute.AdminResponse = dto.AdminNotes ?? "";
                dispute.ResolvedAt = DateTime.UtcNow;

                await _unitOfWork.ClassDisputes.UpdateAsync(dispute);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving dispute {DisputeId}", disputeId);
                return false;
            }
        }

        public async Task<bool> TriggerPayoutAsync(Guid classId)
        {
            try
            {
                var teacherClass = await _unitOfWork.TeacherClasses.GetByIdAsync(classId);
                if (teacherClass == null) return false;

                if (teacherClass.Status != ClassStatus.Completed_PendingPayout) return false;

                var enrollmentCount = await _unitOfWork.ClassEnrollments.GetEnrollmentCountByClassAsync(classId);

                var payout = new TeacherPayout
                {
                    TeacherPayoutId = Guid.NewGuid(),
                    TeacherId = teacherClass.TeacherID,
                    ClassID = classId,
                    FinalAmount = (double)CalculatePayoutAmount(teacherClass, enrollmentCount),
                    Status = DAL.Type.TeacherPayoutStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.TeacherPayouts.CreateAsync(payout);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering payout for class {ClassId}", classId);
                return false;
            }
        }

        private decimal CalculatePayoutAmount(TeacherClass teacherClass, int enrollmentCount)
        {
            var totalRevenue = teacherClass.PricePerStudent * enrollmentCount;
            var platformFee = totalRevenue * 0.15m;
            return totalRevenue - platformFee;
        }
    }
}