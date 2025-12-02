using BLL.IServices.WalletTransactions;
using Common.DTO.WalletTransaction.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controllers.WalletTransaction
{
    [Route("api/wallet-transactions")]
    [ApiController]
    public class WalletTransactionController : ControllerBase
    {
        private readonly IWalletTransactionService _walletTransactionService;

        public WalletTransactionController(IWalletTransactionService walletTransactionService)
        {
            _walletTransactionService = walletTransactionService;
        }
        [Authorize(Roles = "Admin, Teacher")]
        [HttpGet]
        public async Task<IActionResult> GetWalletTransactions([FromQuery] WalletTransactionFilterRequest request)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            var response = await _walletTransactionService.GetWalletTransactionsAsync(userId, request);
            return StatusCode(response.Code, response);
        }
    }
}
