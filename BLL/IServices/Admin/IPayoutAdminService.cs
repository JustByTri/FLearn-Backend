using Common.DTO.ApiResponse;
using Common.DTO.PayOut;

namespace BLL.IServices.Admin
{
    public interface IPayoutAdminService
    {
        /// <summary>
        ///Lấy danh sách yêu cầu rút tiền đang chờ xử lý
        /// </summary>
        Task<BaseResponse<IEnumerable<PayoutRequestDetailDto>>> GetPendingPayoutRequestsAsync(Guid adminUserId);

        /// <summary>
        /// Lấy chi tiết yêu cầu rút tiền theo ID
        /// </summary>
        Task<BaseResponse<PayoutRequestDetailDto>> GetPayoutRequestDetailAsync(Guid adminUserId, Guid payoutRequestId);

        /// <summary>
        /// Xử lý yêu cầu rút tiền (phê duyệt hoặc từ chối) approve or reject
        /// </summary>
        Task<BaseResponse<object>> ProcessPayoutRequestAsync(Guid adminUserId, Guid payoutRequestId, ProcessPayoutRequestDto dto);

        /// <summary>
        /// Lấy lịch sử yêu cầu rút tiền với tùy chọn lọc theo trạng thái
        /// </summary>
        Task<BaseResponse<IEnumerable<PayoutRequestDetailDto>>> GetAllPayoutRequestsAsync(Guid adminUserId, string? status = null);
    }
}
