using BLL.IServices.Payment;
using Common.DTO.Payment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BLL.Services.Payment
{
    public class PayOSService : IPayOSService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PayOSService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _clientId;
        private readonly string _checksumKey;

        public PayOSService(IConfiguration configuration, ILogger<PayOSService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _apiKey = _configuration["PayOs:ApiKey"] ?? string.Empty;
            _clientId = _configuration["PayOs:ClientID"] ?? string.Empty;
            _checksumKey = _configuration["PayOs:CheckSumKey"] ?? string.Empty;
        }

        public async Task<PaymentResponseDto> CreatePaymentLinkAsync(CreatePaymentDto createPaymentDto)
        {
            try
            {
                if (createPaymentDto == null || string.IsNullOrEmpty(createPaymentDto.ItemName))
                {
                    return new PaymentResponseDto
                    {
                        Success = false,
                        ErrorMessage = "Missing required fields"
                    };
                }

                var orderCode = GenerateOrderCode();
                var amount = (int)(createPaymentDto.Amount * 100);
                var returnUrl = _configuration["PaymentOSCallBack:ReturnUrl"] ?? "";
                var cancelUrl = _configuration["PaymentOSCallBack:CancelUrl"] ?? "";

                // Giới hạn description tối đa 25 ký tự (hoặc 9 nếu tài khoản không liên kết PayOS)
                var description = createPaymentDto.Description ?? "Payment";
                if (description.Length > 25)
                {
                    description = description.Substring(0, 25);
                }

                // Tính signature theo đúng format của PayOS
                var signatureData = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
                var signature = GenerateSignature(signatureData);

                _logger.LogInformation("Signature Data: {SignatureData}", signatureData);
                _logger.LogInformation("Signature: {Signature}", signature);

                var paymentData = new
                {
                    orderCode = orderCode,
                    amount = amount,
                    description = description, // Đã được giới hạn
                    items = new[]
                    {
                new
                {
                    name = createPaymentDto.ItemName,
                    quantity = 1,
                    price = amount
                }
            },
                    returnUrl = returnUrl,
                    cancelUrl = cancelUrl,
                    buyerName = createPaymentDto.BuyerName ?? "Customer",
                    buyerEmail = createPaymentDto.BuyerEmail ?? "",
                    buyerPhone = createPaymentDto.BuyerPhone ?? "",
                    expiredAt = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds(),
                    signature = signature
                };

                var jsonPayload = JsonSerializer.Serialize(paymentData);

                _logger.LogInformation("PayOS Request Payload: {Payload}", jsonPayload);

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", _clientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                _httpClient.DefaultRequestHeaders.Add("x-partner-code", "FLearn");

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(
                    "https://api-merchant.payos.vn/v2/payment-requests", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("PayOS Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("PayOS Response Content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var payosResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                        if (!payosResponse.TryGetProperty("data", out var dataElement) ||
                            dataElement.ValueKind == JsonValueKind.Null)
                        {
                            _logger.LogError("PayOS Response missing 'data' property");
                            return new PaymentResponseDto
                            {
                                Success = false,
                                ErrorMessage = "Invalid response from PayOS"
                            };
                        }

                        if (!dataElement.TryGetProperty("checkoutUrl", out var checkoutUrlElement))
                        {
                            _logger.LogError("PayOS Response missing checkoutUrl");
                            return new PaymentResponseDto
                            {
                                Success = false,
                                ErrorMessage = "No checkout URL in response"
                            };
                        }

                        return new PaymentResponseDto
                        {
                            Success = true,
                            PaymentUrl = checkoutUrlElement.GetString() ?? string.Empty,
                            TransactionId = orderCode.ToString(),
                            Amount = createPaymentDto.Amount,
                            ExpiryTime = DateTimeOffset.UtcNow.AddMinutes(15).DateTime,
                            CreatedAt = DateTime.UtcNow,
                            Status = "PENDING"
                        };
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Error parsing PayOS response");
                        return new PaymentResponseDto
                        {
                            Success = false,
                            ErrorMessage = "Error parsing payment response"
                        };
                    }
                }
                else
                {
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        var errorCode = errorResponse.TryGetProperty("code", out var codeElement)
                            ? codeElement.GetString()
                            : "Unknown";
                        var errorDesc = errorResponse.TryGetProperty("desc", out var descElement)
                            ? descElement.GetString()
                            : "Unknown error";

                        _logger.LogError("PayOS Error [{Code}]: {Description}", errorCode, errorDesc);
                    }
                    catch { }

                    return new PaymentResponseDto
                    {
                        Success = false,
                        ErrorMessage = "PayOS Error - Không thể tạo link thanh toán. Vui lòng thử lại."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayOS payment link");
                return new PaymentResponseDto
                {
                    Success = false,
                    ErrorMessage = "Lỗi hệ thống"
                };
            }
        }
        private string GenerateSignature(string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hashBytes).ToLower();
        }

        private long GenerateOrderCode()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

      

        public async Task<PaymentStatusDto> GetPaymentStatusAsync(string transactionId)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", _clientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);

                var response = await _httpClient.GetAsync($"https://api-merchant.payos.vn/v2/payment-requests/{transactionId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var payosResponse = JsonSerializer.Deserialize<JsonElement>(content);
                    var data = payosResponse.GetProperty("data");

                    return new PaymentStatusDto
                    {
                        TransactionId = transactionId,
                        Status = data.GetProperty("status").GetString() ?? "UNKNOWN",
                        Amount = data.GetProperty("amount").GetDecimal() / 100,
                        PaidAt = data.TryGetProperty("transactions", out var transactions) && transactions.GetArrayLength() > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(transactions[0].GetProperty("transactionDateTime").GetInt64()).DateTime
                            : null
                    };
                }

                return new PaymentStatusDto { TransactionId = transactionId, Status = "NOT_FOUND" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status for {TransactionId}", transactionId);
                return new PaymentStatusDto { TransactionId = transactionId, Status = "ERROR" };
            }
        }

        public async Task<bool> ProcessPaymentCallbackAsync(PaymentCallbackDto callbackDto)
        {
            try
            {
              
                _logger.LogInformation("Processing payment callback for transaction {TransactionId} with status {Status}",
                    callbackDto.TransactionId, callbackDto.Status);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment callback for {TransactionId}", callbackDto.TransactionId);
                return false;
            }
        }

        public async Task<bool> RefundPaymentAsync(string transactionId, decimal amount, string reason)
        {
            try
            {
                var refundData = new
                {
                    orderCode = transactionId,
                    amount = (int)(amount * 100),
                    reason = reason
                };

                var jsonPayload = JsonSerializer.Serialize(refundData);

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", _clientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"https://api-merchant.payos.vn/v2/payment-requests/{transactionId}/cancel", content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment {TransactionId}", transactionId);
                return false;
            }
        }
    }
}
