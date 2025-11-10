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