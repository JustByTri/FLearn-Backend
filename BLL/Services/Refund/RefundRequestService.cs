using BLL.IServices.Auth;
using BLL.IServices.Refund;
using BLL.IServices.Upload;
using BLL.IServices.FirebaseService;
using Common.DTO.ApiResponse;
using Common.DTO.Refund;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
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
        private readonly IFirebaseNotificationService _firebaseNotificationService;

        public RefundRequestService(
            IUnitOfWork unitOfWork,
            ILogger<RefundRequestService> logger,
            IEmailService emailService,
            ICloudinaryService cloudinaryService,
            IFirebaseNotificationService firebaseNotificationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _emailService = emailService;
            _cloudinaryService = cloudinaryService;
            _firebaseNotificationService = firebaseNotificationService;
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

                // === GỬI NOTIFICATION CHO HỌC VIÊN KHI ĐƠN ĐƯỢC DUYỆT ===
                if (!string.IsNullOrEmpty(student.FcmToken))
                {
                    try
                    {
                        await _firebaseNotificationService.SendRefundResultToStudentAsync(
                            student.FcmToken,
                            teacherClass?.Title ?? "Lớp học",
                            request.RefundAmount,
                            isApproved: true,
                            reason: null
                        );
                        _logger.LogInformation("[FCM] ✅ Sent refund approval notification to student {StudentId}", student.UserID);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[FCM] ❌ Failed to send refund approval notification");
                    }
                }

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

                // ✅ GỬI EMAIL/FCM KHI REJECT
                // Nếu admin báo sai STK → học viên có thể sửa lại
                bool isBankInfoError = dto.AdminNote.Contains("STK", StringComparison.OrdinalIgnoreCase) ||
                                       dto.AdminNote.Contains("tài khoản", StringComparison.OrdinalIgnoreCase) ||
                                       dto.AdminNote.Contains("ngân hàng", StringComparison.OrdinalIgnoreCase);

                string emailTitle = isBankInfoError 
                    ? "Vui lòng cập nhật lại thông tin ngân hàng" 
                    : "Từ chối hoàn tiền";

                await _emailService.SendRefundRequestRejectedAsync(
                    student.Email,
                    student.UserName,
                    teacherClass?.Title ?? "Lớp học",
                    request.AdminNote
                );

                // === GỬI NOTIFICATION CHO HỌC VIÊN KHI ĐƠN BỊ TỪ CHỐI ===
                if (!string.IsNullOrEmpty(student.FcmToken))
                {
                    try
                    {
                        await _firebaseNotificationService.SendRefundResultToStudentAsync(
                            student.FcmToken,
                            teacherClass?.Title ?? "Lớp học",
                            request.RefundAmount,
                            isApproved: false,
                            reason: dto.AdminNote
                        );
                        _logger.LogInformation("[FCM] ✅ Sent refund rejection notification to student {StudentId}", student.UserID);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[FCM] ❌ Failed to send refund rejection notification");
                    }
                }

                _logger.LogInformation("Đã từ chối đơn hoàn tiền {RefundRequestId} (Lý do: {Reason})", 
                    dto.RefundRequestId, isBankInfoError ? "Sai STK" : "Khác");
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

        public async Task<BaseResponse<IEnumerable<RefundRequestDto>>> GetMyRefundRequestsAsync(Guid learnerId)
        {
            var requests = await _unitOfWork.RefundRequests.GetByLearnerIdAsync(learnerId);


            var myRequests = requests.Where(r => r.EnrollmentID != null);

            var requestsDto = myRequests.Select(r => MapToDto(
                r,
                r.TeacherClass?.Title ?? (r.ClassEnrollment?.Class?.Title ?? "Lớp học không còn tồn tại")
            )).ToList();

            return BaseResponse<IEnumerable<RefundRequestDto>>.Success(requestsDto, "Thành công", 200);
        }

        /// <summary>
        /// Học viên cập nhật thông tin ngân hàng cho đơn hoàn tiền lớp học
        /// (Chuyển từ Draft → Pending khi điền STK)
        /// </summary>
        public async Task<RefundRequestDto> UpdateBankInfoForClassRefundAsync(
            Guid userId,
            Guid refundRequestId,
            UpdateBankInfoDto dto)
        {
            _logger.LogInformation("Student {UserId} is updating bank info for refund request {RefundRequestId}",
                userId, refundRequestId);

            var refundRequest = await _unitOfWork.RefundRequests.GetByIdWithDetailsAsync(refundRequestId);

            if (refundRequest == null)
                throw new KeyNotFoundException("Không tìm thấy đơn hoàn tiền");

            if (refundRequest.StudentID != userId)
                throw new UnauthorizedAccessException("Bạn không có quyền cập nhật đơn này");

            // ✅ CHO PHÉP CẬP NHẬT KHI:
            // - Status = Draft (chưa điền STK)
            // - Status = Pending (admin yêu cầu sửa)
            bool canUpdate = refundRequest.Status == RefundRequestStatus.Draft ||
                             refundRequest.Status == RefundRequestStatus.Pending;

            if (!canUpdate)
                throw new InvalidOperationException("Chỉ có thể cập nhật đơn ở trạng thái Draft hoặc Pending");

            var previousStatus = refundRequest.Status;

            // Cập nhật thông tin ngân hàng
            refundRequest.BankName = dto.BankName;
            refundRequest.BankAccountNumber = dto.BankAccountNumber;
            refundRequest.BankAccountHolderName = dto.BankAccountHolderName;
            refundRequest.UpdatedAt = DateTime.UtcNow;

            // ✨ CHUYỂN TỪ DRAFT → PENDING SAU KHI ĐIỀN STK
            if (refundRequest.Status == RefundRequestStatus.Draft)
            {
                refundRequest.Status = RefundRequestStatus.Pending;
                refundRequest.AdminNote = null;
                _logger.LogInformation("✅ RefundRequest {RefundRequestId} moved from Draft → Pending",
                    refundRequestId);
            }

            await _unitOfWork.RefundRequests.UpdateAsync(refundRequest);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("✅ Updated bank info for refund request {RefundRequestId}", refundRequestId);

            // 📧 Gửi email xác nhận cho học viên
            if (refundRequest.Student != null && !string.IsNullOrEmpty(refundRequest.Student.Email))
            {
                try
                {
                    await _emailService.SendRefundRequestConfirmationAsync(
                        refundRequest.Student.Email,
                        refundRequest.Student.UserName,
                        refundRequest.TeacherClass?.Title ?? "Lớp học",
                        refundRequest.RefundRequestID.ToString()
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send confirmation email");
                }
            }

            // 🔔 GỬI THÔNG BÁO CHO ADMIN (chỉ khi chuyển từ Draft → Pending)
            if (previousStatus == RefundRequestStatus.Draft)
            {
                await SendNotificationToAdminsAsync(refundRequest);
            }

            return MapToDto(refundRequest, refundRequest.Student?.UserName, refundRequest.TeacherClass?.Title);
        }

        /// <summary>
        /// Helper: Gửi Web Push notification cho Admin(s) khi có đơn hoàn tiền mới
        /// </summary>
        private async Task SendNotificationToAdminsAsync(RefundRequest refundRequest)
        {
            try
            {
                // Lấy danh sách Admin
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
                    await _firebaseNotificationService.SendNewRefundRequestToAdminAsync(
                        adminTokens,
                        refundRequest.Student?.UserName ?? "Học viên",
                        refundRequest.TeacherClass?.Title ?? "Lớp học",
                        refundRequest.RefundAmount
                    );

                    _logger.LogInformation("[FCM-Web] ✅ Sent new refund request notification to {Count} admin(s)",
                        adminTokens.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FCM-Web] ❌ Failed to send notification to admins");
            }
        }

        /// <summary>
        /// [ADMIN] Yêu cầu học viên cập nhật lại thông tin ngân hàng
        /// Không reject đơn, chỉ gửi thông báo
        /// </summary>
        public async Task RequestBankInfoUpdateAsync(Guid refundRequestId, Guid adminId, string note)
        {
            _logger.LogInformation(
                "Admin {AdminId} requesting bank info update for RefundRequest {RefundRequestId}",
                adminId, refundRequestId
            );

            var request = await _unitOfWork.RefundRequests.GetByIdWithDetailsAsync(refundRequestId);

            if (request == null)
                throw new KeyNotFoundException("Không tìm thấy đơn hoàn tiền");

            if (request.Status != RefundRequestStatus.Pending && 
                request.Status != RefundRequestStatus.Draft)
                throw new InvalidOperationException(
                    "Chỉ có thể yêu cầu cập nhật cho đơn ở trạng thái Pending hoặc Draft"
                );

            // ✅ KHÔNG THAY ĐỔI STATUS, CHỈ GHI CHÚ
            request.AdminNote = $"[Yêu cầu cập nhật STK] {note}";
            request.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.RefundRequests.UpdateAsync(request);
            await _unitOfWork.SaveChangesAsync();

            // 📧 GỬI EMAIL
            if (request.Student != null && !string.IsNullOrEmpty(request.Student.Email))
            {
                try
                {
                    await _emailService.SendBankInfoUpdateRequestAsync(
                        request.Student.Email,
                        request.Student.UserName,
                        request.TeacherClass?.Title ?? "Lớp học",
                        note
                    );

                    _logger.LogInformation(
                        "[EMAIL] ✅ Sent bank update request to {Email}",
                        request.Student.Email
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[EMAIL] ❌ Failed to send bank update request to {Email}",
                        request.Student.Email
                    );
                }
            }

            // 📲 GỬI FCM NOTIFICATION
            if (request.Student != null && !string.IsNullOrEmpty(request.Student.FcmToken))
            {
                try
                {
                    // TODO: Implement FCM notification
                    // await _firebaseNotificationService.SendNotificationAsync(...)
                    _logger.LogInformation(
                        "[FCM] TODO: Send notification to student {StudentId}",
                        request.StudentID
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[FCM] ❌ Failed to send notification to student {StudentId}",
                        request.StudentID
                    );
                }
            }

            _logger.LogInformation(
                "✅ Admin {AdminId} successfully requested bank info update for RefundRequest {RefundRequestId}",
                adminId, refundRequestId
            );
        }

        /// <summary>
        /// [ADMIN] Xem TẤT CẢ đơn hoàn tiền (cả Class và Course) - Endpoint thống nhất
        /// </summary>
        public async Task<BaseResponse<IEnumerable<UnifiedRefundRequestDto>>> GetAllRefundRequestsAsync(
            RefundRequestStatus? status = null,
            RefundRequestType? type = null,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Admin đang xem tất cả đơn hoàn tiền. Status={Status}, Type={Type}", status, type);

                // Lấy tất cả đơn hoàn tiền từ DB
                var allRefundRequests = await _unitOfWork.RefundRequests.GetAllAsync();

                // Lọc theo status
                if (status.HasValue)
                {
                    allRefundRequests = allRefundRequests.Where(r => r.Status == status.Value).ToList();
                }

                // Lọc theo type
                if (type.HasValue)
                {
                    allRefundRequests = allRefundRequests.Where(r => r.RequestType == type.Value).ToList();
                }

                // Sắp xếp theo thời gian tạo (mới nhất trước)
                var sortedRequests = allRefundRequests.OrderByDescending(r => r.RequestedAt).ToList();

                // Map sang UnifiedRefundRequestDto
                var result = new List<UnifiedRefundRequestDto>();

                foreach (var request in sortedRequests)
                {
                    var student = await _unitOfWork.Users.GetByIdAsync(request.StudentID);
                    var processedByAdmin = request.ProcessedByAdminID.HasValue 
                        ? await _unitOfWork.Users.GetByIdAsync(request.ProcessedByAdminID.Value) 
                        : null;

                    // Xác định loại đơn: Class hay Course
                    string category;
                    string displayTitle;
                    decimal originalAmount = request.RefundAmount;

                    if (request.ClassID.HasValue && request.EnrollmentID.HasValue)
                    {
                        // Đơn hoàn tiền LỚP HỌC
                        category = "Class";
                        var teacherClass = await _unitOfWork.TeacherClasses.FindAsync(c => c.ClassID == request.ClassID);
                        displayTitle = teacherClass?.Title ?? "Lớp học không xác định";
                    }
                    else if (request.PurchaseId.HasValue)
                    {
                        // Đơn hoàn tiền KHOÁ HỌC
                        category = "Course";
                        var purchase = await _unitOfWork.Purchases.FindAsync(p => p.PurchasesId == request.PurchaseId);
                        if (purchase != null)
                        {
                            var course = await _unitOfWork.Courses.GetByIdAsync(purchase.CourseId ?? Guid.Empty);
                            displayTitle = course?.Title ?? "Khoá học không xác định";
                            originalAmount = purchase.FinalAmount;
                        }
                        else
                        {
                            displayTitle = "Khoá học không xác định";
                        }
                    }
                    else
                    {
                        // Trường hợp không xác định
                        category = "Unknown";
                        displayTitle = "Không xác định";
                    }

                    var dto = new UnifiedRefundRequestDto
                    {
                        RefundRequestID = request.RefundRequestID,
                        RefundCategory = category,
                        
                        StudentID = request.StudentID,
                        StudentName = student?.UserName ?? "N/A",
                        StudentEmail = student?.Email ?? "N/A",
                        StudentAvatar = student?.Avatar,
                        
                        ClassID = request.ClassID,
                        ClassName = category == "Class" ? displayTitle : null,
                        PurchaseId = request.PurchaseId,
                        CourseName = category == "Course" ? displayTitle : null,
                        
                        RequestType = request.RequestType,
                        Reason = request.Reason,
                        BankName = request.BankName ?? string.Empty,
                        BankAccountNumber = request.BankAccountNumber ?? string.Empty,
                        BankAccountHolderName = request.BankAccountHolderName ?? string.Empty,
                        Status = request.Status,
                        AdminNote = request.AdminNote,
                        RefundAmount = request.RefundAmount,
                        OriginalAmount = originalAmount,
                        
                        RequestedAt = request.RequestedAt,
                        ProcessedAt = request.ProcessedAt,
                        
                        ProofImageUrl = request.ProofImageUrl,
                        ProcessedByAdminName = processedByAdmin?.UserName,
                        
                        // Meta data
                        DisplayTitle = displayTitle,
                        StatusText = request.Status.ToString(),
                        RequestTypeText = request.RequestType.ToString()
                    };
                    
                    result.Add(dto);
                }

                // Phân trang
                var pagedResult = result
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("Tìm thấy {Total} đơn hoàn tiền. Trả về {Count} đơn (trang {Page})", 
                    result.Count, pagedResult.Count, page);

                return BaseResponse<IEnumerable<UnifiedRefundRequestDto>>.Success(
                    pagedResult, 
                    $"Tìm thấy {result.Count} đơn hoàn tiền", 
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách tất cả đơn hoàn tiền");
                return BaseResponse<IEnumerable<UnifiedRefundRequestDto>>.Error(
                    "Đã xảy ra lỗi khi lấy danh sách đơn hoàn tiền", 
                    500, 
                    ex.Message);
            }
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
    }
}

