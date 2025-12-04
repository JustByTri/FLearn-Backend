using Common.DTO.Learner;
using Common.DTO.Payment;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices.Enrollment
{
    public interface IClassEnrollmentService
    {
        /// <summary>
        /// Bước 1: Tạo link thanh toán
        /// </summary>
        Task<PaymentResponseDto?> CreatePaymentLinkAsync(Guid studentId, Guid classId, User student);

        /// <summary>
        /// Bước 2: Xác nhận enrollment sau khi thanh toán thành công
        /// </summary>
        Task<bool> ConfirmEnrollmentAsync(Guid studentId, Guid classId, string transactionId);

        /// <summary>
        /// Lấy thông tin enrollment
        /// </summary>
        Task<ClassEnrollmentResponseDto?> GetEnrollmentAsync(Guid studentId, Guid enrollmentId);

        /// <summary>
        /// Lấy danh sách lớp học có sẵn theo ngôn ngữ
        /// </summary>
        Task<(List<AvailableClassDto> Classes, int TotalCount)> GetAvailableClassesAsync(Guid? languageId = null, string? status = null, int page = 1, int pageSize = 10);
        /// <summary>
        /// Lấy danh sách lớp học mà sinh viên đã đăng ký
        /// </summary>
        Task<(List<EnrolledClassDto> Classes, int TotalCount)> GetStudentEnrolledClassesAsync(Guid studentId, EnrollmentStatus? status = null, int page = 1, int pageSize = 10);
        
        /// <summary>
        /// Học viên tự hủy đăng ký lớp (trong vòng 3 ngày trước khi lớp bắt đầu)
        /// </summary>
        /// <param name="studentId">ID của học viên</param>
        /// <param name="enrollmentId">ID của enrollment cần hủy</param>
        /// <param name="reason">Lý do hủy (optional)</param>
        /// <returns>True nếu hủy thành công, Exception nếu không đủ điều kiện</returns>
        Task<bool> CancelEnrollmentAsync(Guid studentId, Guid enrollmentId, string? reason = null);
    }

}