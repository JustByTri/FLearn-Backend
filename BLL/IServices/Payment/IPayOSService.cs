using Common.DTO.Payment;

namespace BLL.IServices.Payment
{
    public interface IPayOSService
    {
        Task<PaymentResponseDto> CreatePaymentLinkAsync(CreatePaymentDto createPaymentDto);
        Task<PaymentStatusDto> GetPaymentStatusAsync(string transactionId);
        Task<bool> ProcessPaymentCallbackAsync(PaymentCallbackDto callbackDto);
        Task<bool> RefundPaymentAsync(string transactionId, decimal amount, string reason);
    }
}
