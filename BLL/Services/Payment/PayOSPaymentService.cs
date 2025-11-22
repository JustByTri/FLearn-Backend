using BLL.IServices.Payment;
using BLL.IServices.Wallets;
using Common.DTO.ApiResponse;
using Common.DTO.Payment.Response;
using DAL.Helpers;
using DAL.Type;
using DAL.UnitOfWork;
using Hangfire;
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
        private readonly IWalletService _walletService;
        private readonly ILogger<PayOSPaymentService> _logger;
        private readonly string _apiKey;
        private readonly string _clientId;
        private readonly string _checksumKey;
        public PayOSPaymentService(IConfiguration configuration, IUnitOfWork unitOfWork, ILogger<PayOSPaymentService> logger, IWalletService walletService)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _apiKey = _configuration["PayOs:ApiKey"] ?? string.Empty;
            _clientId = _configuration["PayOs:ClientID"] ?? string.Empty;
            _checksumKey = _configuration["PayOs:CheckSumKey"] ?? string.Empty;
            _walletService = walletService;
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
                        ReturnUrl = "https://f-learn.app/api/payments/return-url",
                        CancelUrl = "https://f-learn.app/api/payments/return-url",
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
            var strategy = _unitOfWork.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    if (payload?.Data == null)
                        return BaseResponse<object>.Success("Invalid payload");

                    var orderCode = payload.Data.OrderCode.ToString();
                    var paymentLinkId = payload.Data.PaymentLinkId;

                    var transaction = await _unitOfWork.PaymentTransactions.Query()
                        .Include(t => t.Purchase)
                            .ThenInclude(p => p.Course)
                        .FirstOrDefaultAsync(t => t.TransactionRef == paymentLinkId
                                               || (t.GatewayResponse != null && t.GatewayResponse.Contains(paymentLinkId)));

                    if (transaction == null)
                    {
                        _logger.LogWarning($"Webhook received but no matching transaction. orderCode={orderCode}, paymentLinkId={paymentLinkId}");
                        return BaseResponse<object>.Success("No transaction matched");
                    }

                    if (payload.Data.Code == "00")
                    {
                        transaction.TransactionStatus = TransactionStatus.Succeeded;
                        transaction.CompletedAt = TimeHelper.GetVietnamTime();

                        if (transaction.Purchase != null)
                        {
                            transaction.Purchase.Status = PurchaseStatus.Completed;
                            transaction.Purchase.StartsAt = TimeHelper.GetVietnamTime();
                            transaction.Purchase.EligibleForRefundUntil = TimeHelper.GetVietnamTime().AddDays(3);
                            if (transaction.Purchase.Course != null)
                            {
                                transaction.Purchase.ExpiresAt = TimeHelper.GetVietnamTime().AddDays(transaction.Purchase.Course.DurationDays);
                            }
                            transaction.Purchase.PaidAt = TimeHelper.GetVietnamTime();
                        }

                        if (transaction.Purchase != null && transaction.Purchase.Course != null)
                        {
                            BackgroundJob.Enqueue<IWalletService>(ws => ws.TransferToAdminWalletAsync(transaction.Purchase.PurchasesId));
                            BackgroundJob.Schedule<IWalletService>(ws => ws.ProcessCourseCreationFeeTransferAsync(transaction.Purchase.PurchasesId), TimeSpan.FromDays(3));
                        }
                    }
                    else
                    {
                        transaction.TransactionStatus = TransactionStatus.Failed;
                        if (transaction.Purchase != null)
                        {
                            transaction.Purchase.Status = PurchaseStatus.Failed;
                        }
                    }

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();


                    return BaseResponse<object>.Success("Callback handled");
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Error when handling callback: {Message}", ex.Message);
                    return BaseResponse<object>.Success("System error while handling callbac");
                }
            });
        }
        public async Task ProcessPaymentFailedAsync(string transactionRef)
        {
            var strategy = _unitOfWork.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var transaction = await _unitOfWork.PaymentTransactions.Query()
                        .Include(t => t.Purchase)
                            .ThenInclude(p => p.Course)
                        .FirstOrDefaultAsync(t => t.TransactionRef == transactionRef
                                               || (t.GatewayResponse != null && t.GatewayResponse.Contains(transactionRef)));

                    if (transaction == null)
                    {
                        _logger.LogWarning("No matching transaction found. TransactionRef = {TransactionRef}", transactionRef);
                        return;
                    }

                    transaction.TransactionStatus = TransactionStatus.Failed;
                    transaction.CreatedAt = TimeHelper.GetVietnamTime();

                    if (transaction.Purchase != null)
                    {
                        transaction.Purchase.Status = PurchaseStatus.Failed;

                        _logger.LogInformation("Payment failed for purchase {PurchaseId}, course {CourseName}",
                            transaction.Purchase.PurchasesId,
                            transaction.Purchase.Course?.Title ?? "Unknown");
                    }

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    _logger.LogInformation("Successfully processed payment failure for TransactionRef: {TransactionRef}", transactionRef);
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Error when processing payment failure for TransactionRef: {TransactionRef}", transactionRef);
                    throw;
                }
            });
        }
        #region Private Methods
        #endregion
    }
}
