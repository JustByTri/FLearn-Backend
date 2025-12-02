using BLL.IServices.WalletTransactions;
using Common.DTO.Paging.Response;
using Common.DTO.WalletTransaction.Request;
using Common.DTO.WalletTransaction.Response;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.WalletTransactions
{
    public class WalletTransactionService : IWalletTransactionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WalletTransactionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResponse<List<WalletTransactionResponse>>> GetWalletTransactionsAsync(Guid userId, WalletTransactionFilterRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.FindAsync(u => u.UserID == userId);
                if (user == null)
                    return PagedResponse<List<WalletTransactionResponse>>.Fail(null, "User not found.", 404);

                var wallet = await _unitOfWork.Wallets.FindAsync(w => w.OwnerId == user.UserID);

                if (wallet == null)
                {
                    var teacher = await _unitOfWork.TeacherProfiles.FindAsync(t => t.UserId == user.UserID);
                    if (teacher != null)
                    {
                        wallet = await _unitOfWork.Wallets.Query()
                            .FirstOrDefaultAsync(w => w.TeacherId == teacher.TeacherId);
                    }
                }

                if (wallet == null)
                    return PagedResponse<List<WalletTransactionResponse>>.Fail(null, "Wallet not found.", 404);

                var query = _unitOfWork.WalletTransactions.Query()
                    .Where(wt => wt.WalletId == wallet.WalletId)
                    .AsNoTracking();

                if (request.TransactionType.HasValue)
                    query = query.Where(wt => wt.TransactionType == request.TransactionType);

                if (request.Status.HasValue)
                    query = query.Where(wt => wt.Status == request.Status);

                if (request.FromDate.HasValue)
                    query = query.Where(wt => wt.CreatedAt >= request.FromDate);

                if (request.ToDate.HasValue)
                    query = query.Where(wt => wt.CreatedAt <= request.ToDate);

                query = request.Sort?.ToLower() switch
                {
                    "amount_desc" => query.OrderByDescending(x => x.Amount),
                    "amount_asc" => query.OrderBy(x => x.Amount),
                    "oldest" => query.OrderBy(x => x.CreatedAt),
                    _ => query.OrderByDescending(x => x.CreatedAt)
                };

                var totalItems = await query.CountAsync();

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(wt => new WalletTransactionResponse
                    {
                        WalletTransactionId = wt.WalletTransactionId,
                        Amount = wt.Amount,
                        TransactionType = wt.TransactionType.ToString(),
                        Status = wt.Status.ToString(),
                        Description = wt.Description,
                        CreatedAt = wt.CreatedAt.ToString("dd-MM-yyyy HH:mm")
                    })
                    .ToListAsync();

                return PagedResponse<List<WalletTransactionResponse>>
                    .Success(items, request.PageNumber, request.PageSize, totalItems);
            }
            catch
            {
                return PagedResponse<List<WalletTransactionResponse>>
                    .Error("An error occurred while retrieving wallet transactions.");
            }
        }
    }
}
