using BLL.IServices.Payment;
using Common.DTO.ApiResponse;
using Common.DTO.Payment.Response;
using DAL.Helpers;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;
using System.Text.Json;

namespace BLL.Services.Payment
{
    public class PayOSPaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PayOSPaymentService> _logger;
        private readonly string _apiKey;
        private readonly string _clientId;
        private readonly string _checksumKey;
        public PayOSPaymentService(IConfiguration configuration, IUnitOfWork unitOfWork, ILogger<PayOSPaymentService> logger)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _apiKey = _configuration["PayOs:ApiKey"] ?? string.Empty;
            _clientId = _configuration["PayOs:ClientID"] ?? string.Empty;
            _checksumKey = _configuration["PayOs:CheckSumKey"] ?? string.Empty;
        }

        public async Task<BaseResponse<PaymentCreateResponse>> CreatePaymentAsync(Guid purchaseId)
        {
            var strategy = _unitOfWork.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var purchase = await _unitOfWork.Purchases.Query()
                        .Include(p => p.User)
                        .Include(p => p.Course)
                        .OrderBy(p => p.CreatedAt)
                        .Where(p => p.PurchasesId == purchaseId)
                        .FirstOrDefaultAsync();

                    if (purchase == null)
                    {
                        return BaseResponse<PaymentCreateResponse>.Fail("No purchase information found");
                    }

                    if (purchase.Status != PurchaseStatus.Pending)
                    {
                        return BaseResponse<PaymentCreateResponse>.Fail("The order is not in payment status");
                    }

                    var existingTransaction = await _unitOfWork.PaymentTransactions.Query()
                    .Where(t => t.PurchaseId == purchaseId && t.TransactionStatus == TransactionStatus.Pending)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                    if (existingTransaction != null && existingTransaction.GatewayResponse != null)
                    {
                        var gatewayResponse = JsonSerializer.Deserialize<CreatePaymentLinkResponse>(existingTransaction.GatewayResponse);
                        if (gatewayResponse == null)
                        {
                            _logger.LogWarning("Could not parse GatewayResponse for transaction {TransactionId}", existingTransaction.TransactionId);
                        }
                        if (gatewayResponse?.ExpiredAt != null)
                        {
                            var expiredAtUtc = DateTimeOffset.FromUnixTimeSeconds(gatewayResponse.ExpiredAt.Value).UtcDateTime;
                            var expiredAtVn = TimeZoneInfo.ConvertTimeFromUtc(expiredAtUtc, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

                            if (expiredAtVn > TimeHelper.GetVietnamTime())
                            {
                                var oldResponse = new PaymentCreateResponse
                                {
                                    PaymentUrl = gatewayResponse.CheckoutUrl,
                                    ExpiresAt = expiredAtVn.ToString("dd-MM-yyyy HH:mm:ss"),
                                    TransactionReference = gatewayResponse.PaymentLinkId
                                };

                                await _unitOfWork.CommitTransactionAsync();
                                return BaseResponse<PaymentCreateResponse>.Success(oldResponse, "Reuse old payment link");
                            }
                        }

                    }

                    var client = new PayOSClient(_clientId, _apiKey, _checksumKey);

                    var orderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    var expiredAtSeconds = new DateTimeOffset(TimeHelper.GetVietnamTime().AddMinutes(15))
                                        .ToUniversalTime().ToUnixTimeSeconds();

                    var paymentRequest = new CreatePaymentLinkRequest
                    {
                        OrderCode = orderCode,
                        Amount = Convert.ToInt64(decimal.Truncate(purchase.FinalAmount)),
                        Description = "flearn hoc phi khoa hoc".ToUpper(),
                        ReturnUrl = _configuration["PaymentOSCallBack:ReturnUrl"] ?? "",
                        CancelUrl = _configuration["PaymentOSCallBack:CancelUrl"] ?? "",
                        BuyerName = purchase.User?.FullName ?? purchase.User?.UserName,
                        BuyerEmail = purchase.User?.Email ?? string.Empty,
                        ExpiredAt = expiredAtSeconds
                    };

                    var paymentLink = await client.PaymentRequests.CreateAsync(paymentRequest);

                    var paymentTransaction = new DAL.Models.PaymentTransaction
                    {
                        TransactionId = Guid.NewGuid(),
                        PurchaseId = purchase.PurchasesId,
                        Amount = purchase.FinalAmount,
                        GatewayResponse = JsonSerializer.Serialize(paymentLink),
                        TransactionRef = paymentLink.PaymentLinkId,
                        TransactionStatus = TransactionStatus.Pending,
                        PaymentMethod = PaymentMethod.PayOS,
                        CurrencyType = CurrencyType.VND,
                        CreatedAt = TimeHelper.GetVietnamTime()
                    };

                    await _unitOfWork.PaymentTransactions.CreateAsync(paymentTransaction);
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();
                    Console.WriteLine($"ExpiredAt raw: {paymentLink.ExpiredAt}");
                    var paymentResponse = new PaymentCreateResponse
                    {
                        PaymentUrl = paymentLink.CheckoutUrl,
                        ExpiresAt = paymentLink.ExpiredAt.HasValue && paymentLink.ExpiredAt.Value > 0
                                    ? $"Thanh toán trước {TimeZoneInfo.ConvertTimeFromUtc(
                                    DateTimeOffset.FromUnixTimeSeconds(paymentLink.ExpiredAt.Value).UtcDateTime,
                                    TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
                                    .ToString("dddd, dd MMMM yyyy, HH:mm:ss", new System.Globalization.CultureInfo("vi-VN"))}" : null,
                        TransactionReference = paymentLink.PaymentLinkId,
                    };

                    return BaseResponse<PaymentCreateResponse>.Success(paymentResponse);
                }
                catch (ApiException ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Error from PayOS API: {Message}", ex.Message);
                    return BaseResponse<PaymentCreateResponse>.Error(ex.Message);
                }
                catch (PayOSException ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Error from PayOS SDK: {Message}", ex.Message);
                    return BaseResponse<PaymentCreateResponse>.Error($"PayOS SDK error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "System error: {Message}", ex.Message);
                    return BaseResponse<PaymentCreateResponse>.Error("An error occurred while generating the payment link.");
                }
            });
        }
        public async Task<BaseResponse<object>> HandleCallbackAsync(PayOSWebhookBody payload)
        {
            if (payload?.Data == null)
                return BaseResponse<object>.Fail("Invalid payload");

            var orderCode = payload.Data.OrderCode.ToString();
            var paymentLinkId = payload.Data.PaymentLinkId;

            var transaction = await _unitOfWork.PaymentTransactions.Query()
                .Include(t => t.Purchase)
                .FirstOrDefaultAsync(t => t.TransactionRef == paymentLinkId
                                       || (t.GatewayResponse != null && t.GatewayResponse.Contains(paymentLinkId)));

            if (transaction == null)
            {
                _logger.LogWarning($"Webhook received but no matching transaction. orderCode={orderCode}, paymentLinkId={paymentLinkId}");
                return BaseResponse<object>.Fail("No transaction matched");
            }

            if (payload.Data.Code == "00")
            {
                transaction.TransactionStatus = TransactionStatus.Succeeded;
                if (transaction.Purchase != null)
                {
                    transaction.Purchase.Status = PurchaseStatus.Completed;
                    transaction.Purchase.PaidAt = TimeHelper.GetVietnamTime();
                }
            }
            else
            {
                transaction.TransactionStatus = TransactionStatus.Failed;
            }

            await _unitOfWork.SaveChangesAsync();

            return BaseResponse<object>.Success("Callback handled");
        }
        public Task<BaseResponse<object>> ProcessRefundAsync(Guid paymentTransactionId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<object>> VerifyPaymentAsync(string transactionReference)
        {
            throw new NotImplementedException();
        }
        #region Private Methods
        #endregion
    }
}
