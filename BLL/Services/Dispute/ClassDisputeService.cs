using BLL.IServices.Dispute;
using BLL.IServices.FirebaseService;
using Common.DTO.Dispute;
using DAL.Helpers;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Dispute
{
    public class ClassDisputeService : IClassDisputeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFirebaseNotificationService _firebaseNotificationService;
        private readonly ILogger<ClassDisputeService> _logger;

        // S? ngày sau khi l?p k?t thúc mà h?c viên có th? khi?u n?i
        private const int DISPUTE_WINDOW_DAYS = 3;

        public ClassDisputeService(
            IUnitOfWork unitOfWork,
            IFirebaseNotificationService firebaseNotificationService,
            ILogger<ClassDisputeService> logger)
        {
            _unitOfWork = unitOfWork;
            _firebaseNotificationService = firebaseNotificationService;
            _logger = logger;
        }

        /// <summary>
        /// H?c viên t?o ??n khi?u n?i sau khi h?c xong
        /// </summary>
        public async Task<DisputeDto> CreateDisputeAsync(Guid studentId, CreateDisputeDto dto)
        {
            // 1. Ki?m tra enrollment t?n t?i và thu?c v? h?c viên
            var enrollment = await _unitOfWork.ClassEnrollments.Query()
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Teacher)
                .Include(e => e.Student)
                .FirstOrDefaultAsync(e => e.EnrollmentID == dto.EnrollmentId);

            if (enrollment == null)
                throw new KeyNotFoundException("Không tìm th?y ??ng ký l?p h?c");

            if (enrollment.StudentID != studentId)
                throw new UnauthorizedAccessException("B?n không có quy?n khi?u n?i cho ??ng ký này");

            // 2. Ki?m tra tr?ng thái enrollment (ph?i ?ã thanh toán ho?c ?ã hoàn thành)
            if (enrollment.Status != EnrollmentStatus.Paid && enrollment.Status != EnrollmentStatus.Completed)
                throw new InvalidOperationException($"Không th? khi?u n?i v?i tr?ng thái ??ng ký: {enrollment.Status}");

            var teacherClass = enrollment.Class;
            if (teacherClass == null)
                throw new KeyNotFoundException("Không tìm th?y thông tin l?p h?c");

            // 3. Ki?m tra l?p ?ã k?t thúc ch?a
            var now = TimeHelper.GetVietnamTime();
            if (teacherClass.EndDateTime > now)
                throw new InvalidOperationException("L?p h?c ch?a k?t thúc. B?n ch? có th? khi?u n?i sau khi l?p k?t thúc.");

            // 4. Ki?m tra còn trong th?i gian khi?u n?i không (3 ngày sau khi l?p k?t thúc)
            var disputeDeadline = teacherClass.EndDateTime.AddDays(DISPUTE_WINDOW_DAYS);
            if (now > disputeDeadline)
                throw new InvalidOperationException(
                    $"Th?i h?n khi?u n?i ?ã h?t. B?n ch? có th? khi?u n?i trong vòng {DISPUTE_WINDOW_DAYS} ngày sau khi l?p k?t thúc.");

            // 5. Ki?m tra ?ã có dispute cho enrollment này ch?a
            var existingDispute = await _unitOfWork.ClassDisputes.Query()
                .FirstOrDefaultAsync(d => d.EnrollmentID == dto.EnrollmentId 
                                       && d.Status != DisputeStatus.Closed
                                       && d.Status != DisputeStatus.Resolved_Refused);

            if (existingDispute != null)
                throw new InvalidOperationException("B?n ?ã có ??n khi?u n?i cho l?p h?c này ?ang ???c x? lý.");

            // 6. T?o dispute m?i
            var dispute = new ClassDispute
            {
                DisputeID = Guid.NewGuid(),
                ClassID = teacherClass.ClassID,
                EnrollmentID = dto.EnrollmentId,
                StudentID = studentId,
                Reason = dto.Reason,
                Description = dto.Description,
                Status = DisputeStatus.Submmitted,
                CreatedAt = now
            };

            await _unitOfWork.ClassDisputes.CreateAsync(dispute);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "? Student {StudentId} created dispute {DisputeId} for class {ClassId}",
                studentId, dispute.DisputeID, teacherClass.ClassID);

            // 7. G?i thông báo cho Admin
            await NotifyAdminsAboutNewDispute(dispute, enrollment, teacherClass);

            return MapToDto(dispute, teacherClass, enrollment);
        }

        /// <summary>
        /// L?y danh sách dispute c?a h?c viên
        /// </summary>
        public async Task<List<DisputeDto>> GetMyDisputesAsync(Guid studentId)
        {
            var disputes = await _unitOfWork.ClassDisputes.Query()
                .Include(d => d.Class)
                .Include(d => d.Enrollment)
                .Include(d => d.ResolvedByAdmin)
                .Where(d => d.StudentID == studentId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return disputes.Select(d => MapToDto(d, d.Class, d.Enrollment)).ToList();
        }

        /// <summary>
        /// L?y chi ti?t m?t dispute
        /// </summary>
        public async Task<DisputeDto?> GetDisputeByIdAsync(Guid studentId, Guid disputeId)
        {
            var dispute = await _unitOfWork.ClassDisputes.Query()
                .Include(d => d.Class)
                .Include(d => d.Enrollment)
                .Include(d => d.ResolvedByAdmin)
                .FirstOrDefaultAsync(d => d.DisputeID == disputeId);

            if (dispute == null)
                return null;

            if (dispute.StudentID != studentId)
                throw new UnauthorizedAccessException("B?n không có quy?n xem ??n khi?u n?i này");

            return MapToDto(dispute, dispute.Class, dispute.Enrollment);
        }

        /// <summary>
        /// H?c viên h?y dispute (n?u ?ang ? tr?ng thái Open/Submitted)
        /// </summary>
        public async Task<bool> CancelDisputeAsync(Guid studentId, Guid disputeId)
        {
            var dispute = await _unitOfWork.ClassDisputes.GetByIdAsync(disputeId);

            if (dispute == null)
                throw new KeyNotFoundException("Không tìm th?y ??n khi?u n?i");

            if (dispute.StudentID != studentId)
                throw new UnauthorizedAccessException("B?n không có quy?n h?y ??n khi?u n?i này");

            if (dispute.Status != DisputeStatus.Open && dispute.Status != DisputeStatus.Submmitted)
                throw new InvalidOperationException(
                    $"Không th? h?y ??n khi?u n?i ?ang ? tr?ng thái: {dispute.Status}");

            dispute.Status = DisputeStatus.Closed;
            dispute.AdminResponse = "H?c viên t? h?y ??n khi?u n?i";
            dispute.ResolvedAt = TimeHelper.GetVietnamTime();

            await _unitOfWork.ClassDisputes.UpdateAsync(dispute);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "? Student {StudentId} cancelled dispute {DisputeId}",
                studentId, disputeId);

            return true;
        }

        #region Private Methods

        private DisputeDto MapToDto(ClassDispute dispute, TeacherClass? teacherClass, ClassEnrollment? enrollment)
        {
            return new DisputeDto
            {
                DisputeId = dispute.DisputeID,
                ClassId = dispute.ClassID,
                ClassName = teacherClass?.Title ?? "Unknown",
                EnrollmentId = dispute.EnrollmentID,
                Reason = dispute.Reason ?? "",
                Description = dispute.Description,
                Status = dispute.Status.ToString(),
                AdminResponse = dispute.AdminResponse,
                ResolvedByAdminName = dispute.ResolvedByAdmin?.FullName ?? dispute.ResolvedByAdmin?.UserName,
                CreatedAt = dispute.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                ResolvedAt = dispute.ResolvedAt?.ToString("dd-MM-yyyy HH:mm"),
                PotentialRefundAmount = enrollment?.AmountPaid
            };
        }

        private async Task NotifyAdminsAboutNewDispute(
            ClassDispute dispute, 
            ClassEnrollment enrollment, 
            TeacherClass teacherClass)
        {
            try
            {
                // L?y danh sách Admin
                var adminRole = await _unitOfWork.Roles.FindAsync(r => r.Name == "Admin");
                if (adminRole == null) return;

                var admins = await _unitOfWork.UserRoles.GetQuery()
                    .Where(ur => ur.RoleID == adminRole.RoleID)
                    .Select(ur => ur.User)
                    .ToListAsync();

                var adminTokens = admins
                    .Where(a => a != null && !string.IsNullOrEmpty(a.FcmToken))
                    .Select(a => a!.FcmToken!)
                    .ToList();

                if (adminTokens.Any())
                {
                    var studentName = enrollment.Student?.FullName ?? enrollment.Student?.UserName ?? "H?c viên";

                    await _firebaseNotificationService.SendMulticastNotificationAsync(
                        adminTokens,
                        "??n khi?u n?i m?i ??",
                        $"H?c viên {studentName} khi?u n?i l?p '{teacherClass.Title}'",
                        new Dictionary<string, string>
                        {
                            { "type", "new_class_dispute" },
                            { "disputeId", dispute.DisputeID.ToString() },
                            { "classId", teacherClass.ClassID.ToString() },
                            { "className", teacherClass.Title ?? "" }
                        }
                    );

                    _logger.LogInformation(
                        "[FCM] ? Sent new dispute notification to {Count} admin(s)",
                        adminTokens.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FCM] ? Failed to send dispute notification to admins");
            }
        }

        #endregion
    }
}
