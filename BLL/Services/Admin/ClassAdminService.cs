using BLL.IServices.Admin;
using Common.DTO.Admin;
using DAL.Models;
using DAL.UnitOfWork;
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

            // Thực hiện hủy lớp bằng cách gọi ExecuteCancellationAsync
            // NOTE: Cần expose ExecuteCancellationAsync từ TeacherClassService hoặc tạo lại logic ở đây
            await ExecuteCancellationLogic(request.ClassId, request.Reason);

            _logger.LogInformation("✅ Manager {ManagerId} approved cancellation request {RequestId}",
                managerId, requestId);

            // TODO: Gửi thông báo cho giáo viên về việc yêu cầu được duyệt

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

            // TODO: Gửi thông báo cho giáo viên về việc yêu cầu bị từ chối

            _logger.LogInformation("❌ Manager {ManagerId} rejected cancellation request {RequestId}",
                managerId, requestId);

            return true;
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

        // ============================================
        // PRIVATE HELPER METHODS
        // ============================================

        /// <summary>
        /// Logic hủy lớp (duplicate từ TeacherClassService)
        /// TODO: Refactor để share logic giữa TeacherClassService và ClassAdminService
        /// </summary>
        private async Task ExecuteCancellationLogic(Guid classId, string reason)
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
                        Reason = reason ?? "Manager approved class cancellation",
                        RefundAmount = enrollment.AmountPaid,
                        Status = RefundRequestStatus.Pending,
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

                    // TODO: Gửi FCM notification cho học viên

                    _logger.LogInformation("[RefundRequest] Created for student {StudentId}, amount: {Amount}",
                        enrollment.StudentID, enrollment.AmountPaid);
                }
            }

            // Cập nhật trạng thái lớp
            teacherClass.Status = ClassStatus.Cancelled;
            teacherClass.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.TeacherClasses.UpdateAsync(teacherClass);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("✅ Class {ClassId} cancelled via Manager approval", classId);
        }
        public ClassAdminService(IUnitOfWork unitOfWork, ILogger<ClassAdminService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
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
                if (dispute == null)
                    return false;

                // Update dispute status based on resolution
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

                _logger.LogInformation("Dispute {DisputeId} resolved with resolution: {Resolution}", disputeId, dto.Resolution);
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
                if (teacherClass == null)
                    return false;

                // Check if class is completed and eligible for payout
                if (teacherClass.Status != ClassStatus.Completed_PendingPayout)
                {
                    _logger.LogWarning("Class {ClassId} is not completed, cannot trigger payout", classId);
                    return false;
                }

                // Get enrollment count for calculation
                var enrollmentCount = await _unitOfWork.ClassEnrollments.GetEnrollmentCountByClassAsync(classId);

                // Create payout record
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

                _logger.LogInformation("Payout triggered for class {ClassId}, amount: {Amount}", classId, payout.FinalAmount);
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
            // Calculate payout amount based on enrollments and platform fee
            // This is a simplified calculation - adjust based on your business logic
            var totalRevenue = teacherClass.PricePerStudent * enrollmentCount;
            var platformFee = totalRevenue * 0.15m; // 15% platform fee
            return totalRevenue - platformFee;
        }
    }
}