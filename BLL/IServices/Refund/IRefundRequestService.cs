using Common.DTO.ApiResponse;
using Common.DTO.Refund;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
