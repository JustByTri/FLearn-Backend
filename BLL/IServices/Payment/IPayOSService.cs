using Common.DTO.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
