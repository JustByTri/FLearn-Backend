using BLL.IServices.Auth;
using BLL.IServices.Refund;
using Common.DTO.ApiResponse;
using Common.DTO.Refund;
using DAL.Models;
using Microsoft.AspNetCore.Http;

namespace BLL.IServices.Refund
{
    public interface IRefundRequestService
    {
        /// <summary>
        /// Admin gửi email thông báo cho học viên cần làm đơn hoàn tiền
        /// </summary>
        Task NotifyStudentToCreateRefundAsync(NotifyRefundRequestDto dto);

        /// <summary>
        /// Học viên tạo một yêu cầu hoàn tiền mới
        /// </summary>
        Task<RefundRequestDto> CreateRefundRequestAsync(CreateRefundRequestDto dto, Guid studentId);

        /// <summary>
        /// Admin lấy danh sách các đơn hoàn tiền (lọc theo trạng thái)
        /// </summary>
        Task<IEnumerable<RefundRequestDto>> GetRefundRequestsAsync(RefundRequestStatus? status);

        /// <summary>
        /// Admin lấy chi tiết một đơn hoàn tiền
        /// </summary>
        Task<RefundRequestDto> GetRefundRequestByIdAsync(Guid refundRequestId);

        /// <summary>
        /// Admin xử lý đơn hoàn tiền (Approve hoặc Reject) và gửi email kết quả
        /// </summary>
        Task<RefundRequestDto> ProcessRefundRequestAsync(ProcessRefundRequestDto dto, Guid adminId);

        /// <summary>
        /// Admin gửi email tùy chỉnh cho học viên về đơn hoàn tiền (deprecated - sử dụng ProcessRefundRequestAsync thay thế)
        /// </summary>
        [Obsolete("Sử dụng ProcessRefundRequestAsync để xử lý và gửi email tự động")]
        Task SendRefundEmailAsync(RefundEmailDto dto);
        Task<BaseResponse<IEnumerable<RefundRequestDto>>> GetMyRefundRequestsAsync(Guid learnerId);

        /// <summary>
        /// Học viên cập nhật thông tin ngân hàng cho đơn hoàn tiền lớp học
        /// (Sau khi lớp bị hủy, học viên cần điền thông tin để nhận tiền)
        /// </summary>
        Task<RefundRequestDto> UpdateBankInfoForClassRefundAsync(Guid userId, Guid refundRequestId, UpdateBankInfoDto dto);

        /// <summary>
        /// [ADMIN] Xem TẤT CẢ đơn hoàn tiền (cả Class và Course) - Endpoint thống nhất
        /// </summary>
        Task<BaseResponse<IEnumerable<UnifiedRefundRequestDto>>> GetAllRefundRequestsAsync(
            RefundRequestStatus? status = null, 
            RefundRequestType? type = null,
            int page = 1,
            int pageSize = 20);
    }
}
