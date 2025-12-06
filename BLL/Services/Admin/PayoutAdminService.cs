using BLL.IServices.Admin;
using BLL.IServices.Auth;
using BLL.IServices.FirebaseService;
using Common.DTO.ApiResponse;
using Common.DTO.PayOut;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

namespace BLL.Services.Admin
{
    public class PayoutAdminService : IPayoutAdminService
    {
        private readonly IUnitOfWork _unit;
        private readonly ILogger<PayoutAdminService> _logger;
        private readonly IEmailService _emailService;
        private readonly IFirebaseNotificationService _notificationService;

        public PayoutAdminService(
            IUnitOfWork unit, 
            ILogger<PayoutAdminService> logger, 
            IEmailService emailService,
            IFirebaseNotificationService notificationService)
        {
            _unit = unit;
            _logger = logger;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        public async Task<BaseResponse<IEnumerable<PayoutRequestDetailDto>>> GetPendingPayoutRequestsAsync(Guid adminUserId)
        {
            try
            {
                // Verify admin role
                if (!await IsAdminAsync(adminUserId))
                {
                    return BaseResponse<IEnumerable<PayoutRequestDetailDto>>.Fail(null, "Bạn không có quyền truy cập.", (int)HttpStatusCode.Forbidden);
                }

                var pendingRequests = await _unit.PayoutRequests.GetByConditionAsync(pr => pr.PayoutStatus == PayoutStatus.Pending);
                var result = new List<PayoutRequestDetailDto>();

                foreach (var request in pendingRequests.OrderBy(r => r.RequestedAt))
                {
                    var teacher = await _unit.TeacherProfiles.GetByIdAsync(request.TeacherId);
                    var user = teacher != null ? await _unit.Users.GetByIdAsync(teacher.UserId) : null;
                    var bankAccount = await _unit.TeacherBankAccounts.GetByIdAsync(request.BankAccountId);

                    result.Add(new PayoutRequestDetailDto
                    {
                        PayoutRequestId = request.PayoutRequestId,
                        TeacherId = request.TeacherId,
                        TeacherName = teacher?.FullName ?? "N/A",
                        TeacherEmail = user?.Email ?? "N/A",
                        BankAccountId = request.BankAccountId,
                        BankName = bankAccount?.BankName ?? string.Empty,
                        BankBranch = bankAccount?.BankBranch ?? string.Empty,
                        AccountNumber = bankAccount?.AccountNumber ?? string.Empty,
                        AccountHolder = bankAccount?.AccountHolder ?? string.Empty,
                        Amount = request.Amount,
                        PayoutStatus = request.PayoutStatus.ToString(),
                        RequestedAt = request.RequestedAt,
                        ApprovedAt = request.ApprovedAt,
                        TransactionRef = request.TransactionRef,
                        Note = request.Note,
                        AdminNote = null
                    });
                }

                return BaseResponse<IEnumerable<PayoutRequestDetailDto>>.Success(result, "Lấy danh sách yêu cầu rút tiền thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending payout requests");
                return BaseResponse<IEnumerable<PayoutRequestDetailDto>>.Error($"Lỗi: {ex.Message}", (int)HttpStatusCode.InternalServerError);
            }
        }

        public async Task<BaseResponse<PayoutRequestDetailDto>> GetPayoutRequestDetailAsync(Guid adminUserId, Guid payoutRequestId)
        {
            try
            {
                if (!await IsAdminAsync(adminUserId))
                {
                    return BaseResponse<PayoutRequestDetailDto>.Fail(null, "Bạn không có quyền truy cập.", (int)HttpStatusCode.Forbidden);
                }

                var request = await _unit.PayoutRequests.GetByIdAsync(payoutRequestId);
                if (request == null)
                {
                    return BaseResponse<PayoutRequestDetailDto>.Fail(null, "Không tìm thấy yêu cầu rút tiền", (int)HttpStatusCode.NotFound);
                }

                var teacher = await _unit.TeacherProfiles.GetByIdAsync(request.TeacherId);
                var user = teacher != null ? await _unit.Users.GetByIdAsync(teacher.UserId) : null;
                var bankAccount = await _unit.TeacherBankAccounts.GetByIdAsync(request.BankAccountId);

                var detail = new PayoutRequestDetailDto
                {
                    PayoutRequestId = request.PayoutRequestId,
                    TeacherId = request.TeacherId,
                    TeacherName = teacher?.FullName ?? "N/A",
                    TeacherEmail = user?.Email ?? "N/A",
                    BankAccountId = request.BankAccountId,
                    BankName = bankAccount?.BankName ?? string.Empty,
                    BankBranch = bankAccount?.BankBranch ?? string.Empty,
                    AccountNumber = bankAccount?.AccountNumber ?? string.Empty,
                    AccountHolder = bankAccount?.AccountHolder ?? string.Empty,
                    Amount = request.Amount,
                    PayoutStatus = request.PayoutStatus.ToString(),
                    RequestedAt = request.RequestedAt,
                    ApprovedAt = request.ApprovedAt,
                    TransactionRef = request.TransactionRef,
                    Note = request.Note
                };

                return BaseResponse<PayoutRequestDetailDto>.Success(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payout request detail {PayoutRequestId}", payoutRequestId);
                return BaseResponse<PayoutRequestDetailDto>.Error($"L?i: {ex.Message}", (int)HttpStatusCode.InternalServerError);
            }
        }

        public async Task<BaseResponse<object>> ProcessPayoutRequestAsync(Guid adminUserId, Guid payoutRequestId, ProcessPayoutRequestDto dto)
        {
            try
            {
                if (!await IsAdminAsync(adminUserId))
                {
                    return BaseResponse<object>.Fail(null, "B?n không có quy?n truy c?p.", (int)HttpStatusCode.Forbidden);
                }

                var payoutRequest = await _unit.PayoutRequests.GetByIdAsync(payoutRequestId);
                if (payoutRequest == null)
                {
                    return BaseResponse<object>.Fail(null, "Không thấy yêu cầu rút.", (int)HttpStatusCode.NotFound);
                }

                if (payoutRequest.PayoutStatus != PayoutStatus.Pending)
                {
                    return BaseResponse<object>.Fail(null, $"Yêu cầu đã được xử lý với trạng thái: {payoutRequest.PayoutStatus}", (int)HttpStatusCode.BadRequest);
                }

                var teacher = await _unit.TeacherProfiles.GetByIdAsync(payoutRequest.TeacherId);
                if (teacher == null)
                {
                    return BaseResponse<object>.Fail(null, "Không tìm thấy giáo viên.", (int)HttpStatusCode.NotFound);
                }

                var wallet = (await _unit.Wallets.GetByConditionAsync(w => w.TeacherId == teacher.TeacherId)).FirstOrDefault();
                if (wallet == null)
                {
                    return BaseResponse<object>.Fail(null, "Không tìm thấy ví của giáo viên.", (int)HttpStatusCode.NotFound);
                }

                // Find the related wallet transaction
                var walletTransaction = (await _unit.WalletTransactions.GetByConditionAsync(
          wt => wt.ReferenceId == payoutRequestId && wt.TransactionType == TransactionType.Withdrawal))
                    .FirstOrDefault();

                if (walletTransaction == null)
                {
                    _logger.LogWarning("Wallet transaction not found for payout request {PayoutRequestId}", payoutRequestId);
                }

                var action = dto.Action.ToLower();

                await _unit.ExecuteInTransactionAsync(async () =>
                          {
                              var now = TimeHelper.GetVietnamTime();

                              if (action == "approve")
                              {
                                  // Admin ?ã chuy?n kho?n thành công
                                  payoutRequest.PayoutStatus = PayoutStatus.Completed;
                                  payoutRequest.ApprovedAt = now;
                                  payoutRequest.ApprovedBy = adminUserId;
                                  payoutRequest.UpdatedAt = now;

                                  if (!string.IsNullOrWhiteSpace(dto.TransactionReference))
                                  {
                                      payoutRequest.TransactionRef = dto.TransactionReference;
                                  }
                                  if (!string.IsNullOrWhiteSpace(dto.AdminNote))
                                  {
                                      payoutRequest.Note = (payoutRequest.Note ?? string.Empty) + $" | Admin: {dto.AdminNote}";
                                  }

                                  // Update wallet transaction status
                                  if (walletTransaction != null)
                                  {
                                      walletTransaction.Status = TransactionStatus.Succeeded;
                                  }

                                  _logger.LogInformation("Payout request {PayoutRequestId} approved by admin {AdminUserId}, amount: {Amount}",
              payoutRequestId, adminUserId, payoutRequest.Amount);
                              }
                              else if (action == "reject")
                              {
                                  // T? ch?i yêu c?u - hoàn ti?n vào ví giáo viên
                                  payoutRequest.PayoutStatus = PayoutStatus.Rejected;
                                  payoutRequest.ApprovedAt = now;
                                  payoutRequest.ApprovedBy = adminUserId;
                                  payoutRequest.UpdatedAt = now;

                                  if (!string.IsNullOrWhiteSpace(dto.AdminNote))
                                  {
                                      payoutRequest.Note = (payoutRequest.Note ?? string.Empty) + $" | Admin (Rejected): {dto.AdminNote}";
                                  }

                                  // Refund to teacher wallet
                                  wallet.TotalBalance += payoutRequest.Amount;
                                  wallet.AvailableBalance += payoutRequest.Amount;
                                  wallet.UpdatedAt = now;

                                  // Update original withdrawal transaction to Failed
                                  if (walletTransaction != null)
                                  {
                                      walletTransaction.Status = TransactionStatus.Failed;
                                  }


                                  var refundTransaction = new WalletTransaction
                                  {
                                      WalletTransactionId = Guid.NewGuid(),
                                      WalletId = wallet.WalletId,
                                      Amount = payoutRequest.Amount,
                                      TransactionType = TransactionType.Refund,
                                      Status = TransactionStatus.Succeeded,
                                      Description = $"Hoàn tiền do yêu cầu bị từ chối: {dto.AdminNote}",
                                      ReferenceId = payoutRequestId,
                                      CreatedAt = now
                                  };
                                  await _unit.WalletTransactions.AddAsync(refundTransaction);

                                  _logger.LogInformation("Payout request {PayoutRequestId} rejected by admin {AdminUserId}, refunded {Amount} to wallet",
        payoutRequestId, adminUserId, payoutRequest.Amount);
                              }
                              else
                              {
                                  throw new ArgumentException($"Invalid action: {dto.Action}. Must be 'approve' or 'reject'.");
                              }

                              await _unit.SaveChangesAsync();
                          });


                try
                {
                    var user = await _unit.Users.GetByIdAsync(teacher.UserId);
                    var teacherEmail = user?.Email;
                    var teacherName = teacher.FullName;

                    if (!string.IsNullOrWhiteSpace(teacherEmail))
                    {
                        if (action == "approve")
                        {
                            var bankAccount = await _unit.TeacherBankAccounts.GetByIdAsync(payoutRequest.BankAccountId);
                            await _emailService.SendPayoutRequestApprovedAsync(
                            teacherEmail,
                            teacherName,
                            payoutRequest.Amount,
                            bankAccount?.BankName ?? "N/A",
                            bankAccount?.AccountNumber ?? "N/A",
                            payoutRequest.TransactionRef,
                            dto.AdminNote
                            );
                            _logger.LogInformation("Sent payout approved email to {Email}", teacherEmail);
                        }
                        else if (action == "reject")
                        {
                            var rejectionReason = dto.AdminNote ?? "Yêu cầu có vấn đề";
                            await _emailService.SendPayoutRequestRejectedAsync(
                            teacherEmail,
                            teacherName,
                            payoutRequest.Amount,
                            rejectionReason
                            );
                            _logger.LogInformation("Sent payout rejected email to {Email}", teacherEmail);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Teacher email not found for payout request {PayoutRequestId}", payoutRequestId);
                    }

                    // === GỬI WEB PUSH NOTIFICATION CHO GIÁO VIÊN ===
                    if (user != null && !string.IsNullOrEmpty(user.FcmToken))
                    {
                        await _notificationService.SendPayoutResultToTeacherAsync(
                            user.FcmToken,
                            payoutRequest.Amount,
                            isApproved: action == "approve",
                            reason: action == "reject" ? dto.AdminNote : null
                        );
                        _logger.LogInformation("[FCM-Web] ✅ Sent payout result notification to teacher {TeacherId}", teacher.TeacherId);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send email/notification for payout request {PayoutRequestId}", payoutRequestId);
                    // Don't fail the whole operation if email fails
                }

                var message = action == "approve"
                ? "Đã duyệt thành công"
                : "Đã từ chối yêu cầu, Tiền sẽ được hoàn lại vào ví";

                return BaseResponse<object>.Success(null, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payout request {PayoutRequestId}", payoutRequestId);
                return BaseResponse<object>.Error($"L?i: {ex.Message}", (int)HttpStatusCode.InternalServerError);
            }
        }

        public async Task<BaseResponse<IEnumerable<PayoutRequestDetailDto>>> GetAllPayoutRequestsAsync(Guid adminUserId, string? status = null)
        {
            try
            {
                if (!await IsAdminAsync(adminUserId))
                {
                    return BaseResponse<IEnumerable<PayoutRequestDetailDto>>.Fail(null, "Bạn không có quyền truy cập.", (int)HttpStatusCode.Forbidden);
                }

                var allRequests = await _unit.PayoutRequests.GetAllAsync();

                if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PayoutStatus>(status, true, out var payoutStatus))
                {
                    allRequests = allRequests.Where(pr => pr.PayoutStatus == payoutStatus).ToList();
                }

                var result = new List<PayoutRequestDetailDto>();

                foreach (var request in allRequests.OrderByDescending(r => r.RequestedAt))
                {
                    var teacher = await _unit.TeacherProfiles.GetByIdAsync(request.TeacherId);
                    var user = teacher != null ? await _unit.Users.GetByIdAsync(teacher.UserId) : null;
                    var bankAccount = await _unit.TeacherBankAccounts.GetByIdAsync(request.BankAccountId);

                    result.Add(new PayoutRequestDetailDto
                    {
                        PayoutRequestId = request.PayoutRequestId,
                        TeacherId = request.TeacherId,
                        TeacherName = teacher?.FullName ?? "N/A",
                        TeacherEmail = user?.Email ?? "N/A",
                        BankAccountId = request.BankAccountId,
                        BankName = bankAccount?.BankName ?? string.Empty,
                        BankBranch = bankAccount?.BankBranch ?? string.Empty,
                        AccountNumber = bankAccount?.AccountNumber ?? string.Empty,
                        AccountHolder = bankAccount?.AccountHolder ?? string.Empty,
                        Amount = request.Amount,
                        PayoutStatus = request.PayoutStatus.ToString(),
                        RequestedAt = request.RequestedAt,
                        ApprovedAt = request.ApprovedAt,
                        TransactionRef = request.TransactionRef,
                        Note = request.Note
                    });
                }

                return BaseResponse<IEnumerable<PayoutRequestDetailDto>>.Success(result, ":ấy danh sách rút tiền thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all payout requests");
                return BaseResponse<IEnumerable<PayoutRequestDetailDto>>.Error($"L?i: {ex.Message}", (int)HttpStatusCode.InternalServerError);
            }
        }

        private async Task<bool> IsAdminAsync(Guid userId)
        {
            var userRoles = await _unit.UserRoles.GetByConditionAsync(ur => ur.UserID == userId);
            var roleIds = userRoles.Select(ur => ur.RoleID).ToList();
            var roles = await _unit.Roles.GetAllAsync();

            return roles.Any(r => roleIds.Contains(r.RoleID) &&
             (r.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                        r.Name.Equals("Manager", StringComparison.OrdinalIgnoreCase)));
        }
    }
}
