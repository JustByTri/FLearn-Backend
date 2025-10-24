using BLL.IServices.Auth;
using BLL.IServices.Refund;
using Common.DTO.Refund;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.Refund
{
    public class RefundRequestService : IRefundRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<RefundRequestService> _logger;
        private readonly IWebHostEnvironment _environment;

        public RefundRequestService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<RefundRequestService> logger,
            IWebHostEnvironment environment)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
            _environment = environment;
        }

        public async Task<RefundRequestDto> CreateRefundRequestAsync(
            Guid studentId,
            CreateRefundRequestDto dto)
        {
            try
            {
                // Validate enrollment exists and belongs to student
                var enrollment = await _unitOfWork.ClassEnrollments
                    .GetEnrollmentWithDetailsAsync(dto.EnrollmentID);

                if (enrollment == null || enrollment.StudentID != studentId)
                {
                    throw new UnauthorizedAccessException("Không tìm thấy đăng ký lớp học này");
                }

                if (enrollment.Status != EnrollmentStatus.Paid)
                {
                    throw new InvalidOperationException("Chỉ có thể yêu cầu hoàn tiền cho lớp đã thanh toán");
                }

                // Check if already has pending refund request
                var existingRequest = await _unitOfWork.RefundRequests
                    .GetPendingRefundByEnrollmentAsync(dto.EnrollmentID);

                if (existingRequest != null)
                {
                    throw new InvalidOperationException("Bạn đã có yêu cầu hoàn tiền đang chờ xử lý");
                }

              
                // Create refund request
                var refundRequest = new RefundRequest
                {
                    RefundRequestID = Guid.NewGuid(),
                    EnrollmentID = dto.EnrollmentID,
                    StudentID = studentId,
                    ClassID = enrollment.ClassID,
                    RequestType = dto.RequestType,
                    BankName = dto.BankName,
                    BankAccountNumber = dto.BankAccountNumber,
                    BankAccountHolderName = dto.BankAccountHolderName,
                    Reason = dto.Reason,
                  
                    RefundAmount = enrollment.AmountPaid,
                    Status = RefundRequestStatus.Pending,
                    RequestedAt = DateTime.UtcNow
                };

                await _unitOfWork.RefundRequests.CreateAsync(refundRequest);
                await _unitOfWork.SaveChangesAsync();

                // Send notification email
                await _emailService.SendRefundRequestConfirmationAsync(
                    enrollment.Student.Email,
                    enrollment.Student.FullName,
                    enrollment.Class.Title,
                    refundRequest.RefundRequestID.ToString()
                );

                _logger.LogInformation(
                    "📝 Student {StudentId} created refund request {RefundRequestId}",
                    studentId,
                    refundRequest.RefundRequestID
                );

                return MapToDto(refundRequest, enrollment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refund request");
                throw;
            }
        }

        private async Task<string> UploadProofImageAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "refund-proofs");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/refund-proofs/{uniqueFileName}";
        }

        private RefundRequestDto MapToDto(RefundRequest request, ClassEnrollment enrollment)
        {
            return new RefundRequestDto
            {
                RefundRequestID = request.RefundRequestID,
                EnrollmentID = request.EnrollmentID,
                StudentID = request.StudentID,
                StudentName = enrollment.Student.FullName,
                StudentEmail = enrollment.Student.Email,
                ClassID = request.ClassID,
                ClassName = enrollment.Class.Title,
                RequestType = request.RequestType,
                RequestTypeDisplay = GetRequestTypeDisplay(request.RequestType),
                BankName = request.BankName,
                BankAccountNumber = request.BankAccountNumber,
                BankAccountHolderName = request.BankAccountHolderName,
                Reason = request.Reason,
              
                Status = request.Status,
                StatusDisplay = GetStatusDisplay(request.Status),
                AdminNote = request.AdminNote,
                RefundAmount = request.RefundAmount,
                RequestedAt = request.RequestedAt,
                ProcessedAt = request.ProcessedAt
            };
        }

        private string GetRequestTypeDisplay(RefundRequestType type)
        {
            return type switch
            {
                RefundRequestType.ClassCancelled_InsufficientStudents => "Lớp học bị hủy - Không đủ học viên",
                RefundRequestType.ClassCancelled_TeacherUnavailable => "Lớp học bị hủy - Giáo viên không có mặt",
                RefundRequestType.StudentPersonalReason => "Lý do cá nhân",
                RefundRequestType.ClassQualityIssue => "Vấn đề chất lượng lớp học",
                RefundRequestType.TechnicalIssue => "Sự cố kỹ thuật",
                _ => "Khác"
            };
        }

        private string GetStatusDisplay(RefundRequestStatus status)
        {
            return status switch
            {
                RefundRequestStatus.Pending => "Chờ xử lý",
                RefundRequestStatus.UnderReview => "Đang xem xét",
                RefundRequestStatus.Approved => "Đã chấp nhận",
                RefundRequestStatus.Rejected => "Từ chối",
                RefundRequestStatus.Completed => "Hoàn thành",
                RefundRequestStatus.Cancelled => "Đã hủy",
                _ => "Không xác định"
            };
        }
    }
}
