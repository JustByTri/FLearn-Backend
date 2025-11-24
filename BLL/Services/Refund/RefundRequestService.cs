using BLL.IServices.Auth;
using BLL.IServices.Refund;
using BLL.IServices.Upload;
using Common.DTO.ApiResponse;
using Common.DTO.Refund;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.Extensions.Logging;
using System.Net;

namespace BLL.Services.Refund
{
    public class RefundRequestService : IRefundRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RefundRequestService> _logger;
        private readonly IEmailService _emailService;
        private readonly ICloudinaryService _cloudinaryService;

        public RefundRequestService(
            IUnitOfWork unitOfWork,
            ILogger<RefundRequestService> logger,
            IEmailService emailService,
            ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _emailService = emailService;
            _cloudinaryService = cloudinaryService;
        }

        /// <summary>
        /// 1. Admin gửi email thông báo học viên cần làm đơn hoàn tiền
        /// </summary>
        public async Task NotifyStudentToCreateRefundAsync(NotifyRefundRequestDto dto)
        {
            _logger.LogInformation("Admin đang gửi thông báo hoàn tiền cho học viên {StudentId} về lớp {ClassId}",
                dto.StudentId, dto.ClassId);

            // Lấy thông tin học viên
            var student = await _unitOfWork.Users.GetByIdAsync(dto.StudentId);
            if (student == null || string.IsNullOrEmpty(student.Email))
            {
                throw new InvalidOperationException("Không tìm thấy thông tin học viên hoặc email.");
            }

            // Gửi email hướng dẫn
            var sent = await _emailService.SendRefundRequestInstructionAsync(
                student.Email,
                student.UserName,
                dto.ClassName,
                dto.ClassStartDateTime,
                dto.Reason
            );

            if (!sent)
            {
                throw new Exception("Gửi email thất bại.");
            }

            _logger.LogInformation("Đã gửi email thông báo hoàn tiền thành công tới {Email}", student.Email);
        }

        /// <summary>
        /// 2. Học viên tạo đơn hoàn tiền
        /// </summary>
        public async Task<RefundRequestDto> CreateRefundRequestAsync(CreateRefundRequestDto dto, Guid studentId)
        {
            _logger.LogInformation("Học viên {StudentId} đang tạo đơn hoàn tiền cho Lớp {ClassId}", studentId, dto.ClassID);

            // 1. Kiểm tra Enrollment
            var enrollment = await _unitOfWork.ClassEnrollments.GetByIdAsync(dto.EnrollmentID);
            if (enrollment == null || enrollment.StudentID != studentId || enrollment.ClassID != dto.ClassID)
            {
                throw new ArgumentException("Thông tin đăng ký lớp học không hợp lệ.");
            }

            // 2. Kiểm tra xem đã có đơn đang chờ chưa
            var existingRequest = await _unitOfWork.RefundRequests.GetPendingRefundByEnrollmentAsync(dto.EnrollmentID);
            if (existingRequest != null)
            {
                throw new InvalidOperationException("Bạn đã có một đơn hoàn tiền đang chờ xử lý cho lớp học này.");
            }

            // 3. Lấy thông tin học viên để gửi email
            var student = await _unitOfWork.Users.GetByIdAsync(studentId);

            // 4. Tạo đơn mới
            var newRequest = new RefundRequest
            {
                RefundRequestID = Guid.NewGuid(),
                EnrollmentID = dto.EnrollmentID,
                StudentID = studentId,
                ClassID = dto.ClassID,
                RequestType = dto.RequestType,
                Reason = (dto.RequestType == RefundRequestType.Other) ? dto.Reason : dto.RequestType.ToString(),
                BankName = dto.BankName,
                BankAccountNumber = dto.BankAccountNumber,
                BankAccountHolderName = dto.BankAccountHolderName,
                RefundAmount = enrollment.AmountPaid,
                Status = RefundRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.RefundRequests.CreateAsync(newRequest);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Đã tạo đơn {RefundRequestID} thành công.", newRequest.RefundRequestID);

            // 5. Gửi email xác nhận đã nhận đơn
            if (student != null && !string.IsNullOrEmpty(student.Email))
            {
                await _emailService.SendRefundRequestConfirmationAsync(
                    student.Email,
                    student.UserName,
                    dto.ClassName,
                    newRequest.RefundRequestID.ToString()
                );
            }

            return MapToDto(newRequest, student?.UserName, dto.ClassName);
        }

        /// <summary>
        /// 3. Admin xem danh sách đơn hoàn tiền
        /// </summary>
        public async Task<IEnumerable<RefundRequestDto>> GetRefundRequestsAsync(RefundRequestStatus? status)
        {
            // Lấy đơn theo status
            var requests = status.HasValue
                ? await _unitOfWork.RefundRequests.GetRefundRequestsByStatusAsync(status.Value)
                : (await _unitOfWork.RefundRequests.GetAllAsync()).ToList();

            // Sắp xếp theo ngày yêu cầu
            var sortedRequests = requests.OrderByDescending(r => r.RequestedAt).ToList();

            // Map sang DTO với thông tin đầy đủ
            var result = new List<RefundRequestDto>();
            foreach (var request in sortedRequests)
            {
                var student = await _unitOfWork.Users.GetByIdAsync(request.StudentID);
                var teacherClass = await _unitOfWork.TeacherClasses.FindAsync(c => c.ClassID == request.ClassID);

                var dto = MapToDto(request, student?.UserName, teacherClass?.Title);
                result.Add(dto);
            }

            return result;
        }

        /// <summary>
        /// 4. Admin xem chi tiết một đơn hoàn tiền
        /// </summary>
        public async Task<RefundRequestDto> GetRefundRequestByIdAsync(Guid refundRequestId)
        {
            var request = await _unitOfWork.RefundRequests.GetByIdWithDetailsAsync(refundRequestId);
            if (request == null)
            {
                throw new KeyNotFoundException("Không tìm thấy đơn hoàn tiền.");
            }

            var student = await _unitOfWork.Users.GetByIdAsync(request.StudentID);
            var teacherClass = await _unitOfWork.TeacherClasses.FindAsync(c => c.ClassID == request.ClassID);

            return MapToDto(request, student?.UserName, teacherClass?.Title);
        }

        /// <summary>
        /// 5. Admin xử lý đơn hoàn tiền (Approve hoặc Reject)
        /// </summary>
        public async Task<RefundRequestDto> ProcessRefundRequestAsync(ProcessRefundRequestDto dto, Guid adminId)
        {
            _logger.LogInformation("Admin {AdminId} đang xử lý đơn hoàn tiền {RefundRequestId}",
                adminId, dto.RefundRequestId);

            // 1. Lấy thông tin đơn
            var request = await _unitOfWork.RefundRequests.GetByIdWithDetailsAsync(dto.RefundRequestId);
            if (request == null)
            {
                throw new KeyNotFoundException("Không tìm thấy đơn hoàn tiền.");
            }

            if (request.Status != RefundRequestStatus.Pending)
            {
                throw new InvalidOperationException($"Đơn hoàn tiền đã được xử lý với trạng thái: {request.Status}");
            }

            // 2. Lấy thông tin học viên và lớp học
            var student = await _unitOfWork.Users.GetByIdAsync(request.StudentID);
            var teacherClass = await _unitOfWork.TeacherClasses.FindAsync(c => c.ClassID == request.ClassID);

            if (student == null || string.IsNullOrEmpty(student.Email))
            {
                throw new InvalidOperationException("Không tìm thấy thông tin học viên.");
            }

            // 3. Xử lý theo action
            if (dto.Action == ProcessAction.Approve)
            {
                // Upload hình ảnh chứng minh (nếu có)
                string? proofImageUrl = null;
                if (dto.ProofImage != null)
                {
                    proofImageUrl = await _cloudinaryService.UploadFileAsync(dto.ProofImage, "refund-proofs");
                }

                // Cập nhật đơn
                request.Status = RefundRequestStatus.Approved;
                request.ProofImageUrl = proofImageUrl;
                request.AdminNote = dto.AdminNote ?? "Đơn hoàn tiền đã được chấp nhận.";
                request.ProcessedAt = DateTime.UtcNow;
                request.ProcessedByAdminID = adminId;
                request.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.RefundRequests.UpdateAsync(request);
                await _unitOfWork.SaveChangesAsync();

                // Gửi email thông báo approved
                await _emailService.SendRefundRequestApprovedAsync(
                    student.Email,
                    student.UserName,
                    teacherClass?.Title ?? "Lớp học",
                    request.RefundAmount,
                    proofImageUrl,
                    request.AdminNote
                );

                _logger.LogInformation("Đã chấp nhận đơn hoàn tiền {RefundRequestId}", dto.RefundRequestId);
            }
            else if (dto.Action == ProcessAction.Reject)
            {
                if (string.IsNullOrEmpty(dto.AdminNote))
                {
                    throw new ArgumentException("Vui lòng cung cấp lý do từ chối.");
                }

                // Cập nhật đơn
                request.Status = RefundRequestStatus.Rejected;
                request.AdminNote = dto.AdminNote;
                request.ProcessedAt = DateTime.UtcNow;
                request.ProcessedByAdminID = adminId;
                request.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.RefundRequests.UpdateAsync(request);
                await _unitOfWork.SaveChangesAsync();

                // Gửi email thông báo rejected
                await _emailService.SendRefundRequestRejectedAsync(
                    student.Email,
                    student.UserName,
                    teacherClass?.Title ?? "Lớp học",
                    request.AdminNote
                );

                _logger.LogInformation("Đã từ chối đơn hoàn tiền {RefundRequestId}", dto.RefundRequestId);
            }

            return MapToDto(request, student.UserName, teacherClass?.Title);
        }

        /// <summary>
        /// [DEPRECATED] Admin gửi email tùy chỉnh
        /// </summary>
        [Obsolete("Sử dụng ProcessRefundRequestAsync thay thế")]
        public async Task SendRefundEmailAsync(RefundEmailDto dto)
        {
            var request = await _unitOfWork.RefundRequests.GetByIdAsync(dto.RefundRequestId);
            if (request == null)
            {
                throw new KeyNotFoundException("Không tìm thấy đơn hoàn tiền.");
            }

            var student = await _unitOfWork.Users.GetByIdAsync(request.StudentID);
            if (student == null || string.IsNullOrEmpty(student.Email))
            {
                throw new InvalidOperationException("Học viên này không có thông tin email.");
            }

            _logger.LogInformation("Admin đang gửi email cho {Email} về đơn {RefundRequestId}",
                student.Email, request.RefundRequestID);

            // Note: Method này được deprecated, nên chỉ log warning
            _logger.LogWarning("SendRefundEmailAsync is deprecated. Use ProcessRefundRequestAsync instead.");
        }

        /// <summary>
        /// Hàm helper để map Model sang DTO
        /// </summary>
        private RefundRequestDto MapToDto(RefundRequest request, string? studentName = null, string? className = null)
        {
            return new RefundRequestDto
            {
                RefundRequestID = request.RefundRequestID,

            
                EnrollmentID = request.EnrollmentID ?? Guid.Empty,
                ClassID = request.ClassID ?? Guid.Empty,
           

                StudentID = request.StudentID,
                StudentName = studentName ?? request.Student?.UserName ?? "N/A",

                ClassName = className ?? request.TeacherClass?.Title ?? "N/A",

                RequestType = request.RequestType,
                Reason = request.Reason,
                BankName = request.BankName,
                BankAccountNumber = request.BankAccountNumber,
                BankAccountHolderName = request.BankAccountHolderName,
                Status = request.Status,
                RefundAmount = request.RefundAmount,
                RequestedAt = request.RequestedAt,

              
                ProcessedAt = request.ProcessedAt,

                AdminNote = request.AdminNote,
                ProofImageUrl = request.ProofImageUrl
            };
        }
        public async Task<BaseResponse<IEnumerable<RefundRequestDto>>> GetMyRefundRequestsAsync(Guid learnerId)
        {
            var requests = await _unitOfWork.RefundRequests.GetByLearnerIdAsync(learnerId);

          
            var requestsDto = requests.Select(r => MapToDto(
                r,
                r.Student?.UserName,
                r.TeacherClass?.Title ?? (r.ClassEnrollment?.Class?.Title ?? "Lớp học không còn tồn tại")
            )).ToList();

            return BaseResponse<IEnumerable<RefundRequestDto>>.Success(requestsDto, "Thành công", 200);
        }
    }
}

