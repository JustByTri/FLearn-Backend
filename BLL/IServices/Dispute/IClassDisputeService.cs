using Common.DTO.Dispute;

namespace BLL.IServices.Dispute
{
    public interface IClassDisputeService
    {
        /// <summary>
        /// H?c viên t?o ??n khi?u n?i sau khi h?c xong
        /// </summary>
        Task<DisputeDto> CreateDisputeAsync(Guid studentId, CreateDisputeDto dto);

        /// <summary>
        /// L?y danh sách dispute c?a h?c viên
        /// </summary>
        Task<List<DisputeDto>> GetMyDisputesAsync(Guid studentId);

        /// <summary>
        /// L?y chi ti?t m?t dispute
        /// </summary>
        Task<DisputeDto?> GetDisputeByIdAsync(Guid studentId, Guid disputeId);

        /// <summary>
        /// H?c viên h?y dispute (n?u ?ang ? tr?ng thái Open)
        /// </summary>
        Task<bool> CancelDisputeAsync(Guid studentId, Guid disputeId);
    }
}
