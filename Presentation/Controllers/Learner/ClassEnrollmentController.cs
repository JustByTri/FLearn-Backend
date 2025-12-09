using BLL.IServices.Enrollment;
using BLL.IServices.Payment;
using Common.DTO.Learner;
using Common.DTO.Payment;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;
using System.Security.Claims;

namespace Presentation.Controllers.Learner
{
    [Route("api/student/classes")]
    [ApiController]
    [Authorize] 
    public class ClassEnrollmentController : ControllerBase
    {
        private readonly IClassEnrollmentService _enrollmentService;
        private readonly IPayOSService _payOSService;
        private readonly ILogger<ClassEnrollmentController> _logger;

        public ClassEnrollmentController(
            IClassEnrollmentService enrollmentService,
            IPayOSService payOSService,
            ILogger<ClassEnrollmentController> logger)
        {
            _enrollmentService = enrollmentService;
            _payOSService = payOSService;
            _logger = logger;
        }

        /// <summary>
        /// Bước 1: Student yêu cầu enroll - tạo link thanh toán (chỉ Learner)
        /// </summary>
        [HttpPost("{classId:guid}/enroll")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> EnrollClass(Guid classId)
        {
            try
            {
                var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Lấy thông tin student từ claims hoặc database
                var student = new User
                {
                    UserID = studentId,
                    FullName = User.Identity?.Name ?? "Student",
                    Email = User.FindFirstValue(ClaimTypes.Email) ?? "",

                };

                var paymentResponse = await _enrollmentService.CreatePaymentLinkAsync(
                    studentId, classId, student);

                if (paymentResponse == null)
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Failed to create payment link"
                    });

                return Ok(new
                {
                    success = true,
                    message = "Payment link created. Please complete payment to confirm enrollment.",
                    data = new
                    {
                        paymentUrl = paymentResponse.PaymentUrl,
                        transactionId = paymentResponse.TransactionId,
                        amount = paymentResponse.Amount,
                        expiryTime = paymentResponse.ExpiryTime
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// Bước 2: Callback từ PayOS - xác nhận enrollment
        /// </summary>
        [HttpPost("payment-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCallback([FromBody] PaymentCallbackDto callbackDto)
        {
            try
            {
                // Kiểm tra thanh toán thành công
                if (callbackDto.Status != "PAID")
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Payment not successful"
                    });
                }

                // Lấy studentId và classId từ metadata của PayOS
                // Giả định PayOS trả về thông tin này
                if (!Guid.TryParse(callbackDto.StudentID?.ToString(), out var studentId))
                    return BadRequest(new { success = false, message = "Invalid student ID" });

                if (!Guid.TryParse(callbackDto.ClassID?.ToString(), out var classId))
                    return BadRequest(new { success = false, message = "Invalid class ID" });

                // Xác nhận enrollment
                var result = await _enrollmentService.ConfirmEnrollmentAsync(
                    studentId, classId, callbackDto.TransactionId);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Enrollment confirmed successfully"
                    });
                }

                return BadRequest(new { success = false, message = "Failed to confirm enrollment" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái enrollment (Learner hoặc Teacher)
        /// </summary>
        [HttpGet("enrollments/{enrollmentId:guid}")]
        [Authorize(Roles = "Learner,Teacher")]
        public async Task<IActionResult> GetEnrollmentStatus(Guid enrollmentId)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var enrollment = await _enrollmentService.GetEnrollmentAsync(studentId, enrollmentId);

            if (enrollment == null)
                return NotFound(new { success = false, message = "Enrollment not found" });

            return Ok(new { success = true, data = enrollment });
        }

        /// <summary>
        /// Lấy danh sách lớp học theo ngôn ngữ (Public - không cần auth)
        /// </summary>
        [HttpGet("available")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailableClasses([FromQuery] Guid? languageId = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var classes = await _enrollmentService.GetAvailableClassesAsync(languageId, status, page, pageSize);

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách lớp học thành công",
                    data = classes.Classes,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = classes.TotalCount,
                        totalPages = (int)Math.Ceiling((double)classes.TotalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy danh sách lớp học" });
            }
        }

        /// <summary>
        /// Lấy danh sách lớp học mà user đã đăng ký (Learner hoặc Teacher đều xem được)
        /// </summary>
        [HttpGet("my-enrollments")]
        [Authorize(Roles = "Learner,Teacher")]
        public async Task<IActionResult> GetMyEnrolledClasses([FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Parse enrollment status if provided
                EnrollmentStatus? enrollmentStatus = null;
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<EnrollmentStatus>(status, true, out var parsedStatus))
                {
                    enrollmentStatus = parsedStatus;
                }

                var result = await _enrollmentService.GetStudentEnrolledClassesAsync(studentId, enrollmentStatus, page, pageSize);

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách lớp học đã đăng ký thành công",
                    data = result.Classes,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = result.TotalCount,
                        totalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize)
                    },
                    summary = new
                    {
                        totalEnrollments = result.TotalCount,
                        paidEnrollments = result.Classes.Count(c => c.EnrollmentStatus == "Paid"),
                        activeClasses = result.Classes.Count(c => c.CanJoinClass),
                        completedClasses = result.Classes.Count(c => c.IsClassFinished)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy danh sách lớp học đã đăng ký" });
            }
        }

        /// <summary>
        /// [Học viên] Hủy đăng ký lớp học (chỉ Learner)
        /// Tự động tạo RefundRequest và yêu cầu học viên cập nhật thông tin ngân hàng
        /// </summary>
        /// <param name="enrollmentId">ID của enrollment cần hủy</param>
        /// <param name="dto">Lý do hủy (optional)</param>
        [HttpDelete("enrollments/{enrollmentId:guid}")]
        [Authorize(Roles = "Learner")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> CancelEnrollment(
            Guid enrollmentId,
            [FromBody] CancelEnrollmentRequestDto dto)
        {
            try
            {
                if (!this.TryGetUserId(out var studentId, out var error))
                    return error!;

                var result = await _enrollmentService.CancelEnrollmentAsync(
                    studentId,
                    enrollmentId,
                    dto?.Reason);

                return Ok(new
                {
                    success = true,
                    message = "Hủy đăng ký thành công. Vui lòng cập nhật thông tin ngân hàng để nhận hoàn tiền.",
                    hint = "Bạn có thể cập nhật thông tin ngân hàng trong mục 'Đơn hoàn tiền của tôi'."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling enrollment {EnrollmentId}", enrollmentId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }
    }
}