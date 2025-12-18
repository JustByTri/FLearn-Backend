using BLL.IServices.Wallets;
using BLL.IServices.WalletTransactions;
using DAL.Type;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controllers.Teacher
{
    /// <summary>
    /// Controller quản lý ví c?a giáo viên
    /// 
    /// LUỒNG THANH TOÁN LỚP H?C:
    /// ???????????????????????????????????????????????????????????????????????????????
    /// ?  1. H?c viên ??ng ký l?p ? Ti?n vào ví Admin                               ?
    /// ?  2. L?p k?t thúc ? 3 ngày dispute window                                    ?
    /// ?  3. N?u có dispute ? Ch? resolve + 3 ngày                                   ?
    /// ?  4. Tính ti?n: TotalRevenue - DisputeRefunds = NetRevenue                   ?
    /// ?  5. Teacher nh?n: NetRevenue * 90%                                          ?
    /// ?  6. Admin gi?: NetRevenue * 10% (platform fee)                              ?
    /// ???????????????????????????????????????????????????????????????????????????????
    /// 
    /// ENDPOINTS:
    /// - GET  /api/teacher/wallet              ? Xem s? d? ví
    /// - GET  /api/teacher/wallet/transactions ? L?ch s? giao d?ch (payout, withdraw, refund)
    /// - GET  /api/teacher/wallet/payouts      ? L?ch s? thanh toán t? l?p h?c
    /// - POST /api/teacher/wallet/withdraw     ? Yêu c?u rút ti?n
    /// - GET  /api/teacher/wallet/withdrawal-requests ? Danh sách yêu c?u rút ti?n
    /// 
    /// TODO: Implement các methods trong IWalletService và IWalletTransactionService
    /// </summary>
    [Route("api/teacher/wallet")]
    [ApiController]
    [Authorize(Roles = "Teacher")]
    public class TeacherWalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly IWalletTransactionService _transactionService;
        private readonly ILogger<TeacherWalletController> _logger;

        public TeacherWalletController(
            IWalletService walletService,
            IWalletTransactionService transactionService,
            ILogger<TeacherWalletController> logger)
        {
            _walletService = walletService;
            _transactionService = transactionService;
            _logger = logger;
        }

        /// <summary>
        /// [Giáo viên] Xem thông tin ví c?a mình
        /// - TotalBalance: T?ng s? d?
        /// - AvailableBalance: S? d? kh? d?ng (có th? rút)
        /// - HoldBalance: S? d? t?m gi?
        /// 
        /// TODO: Implement GetTeacherWalletAsync trong IWalletService
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetMyWallet()
        {
            // TODO: Implement
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            return Ok(new
            {
                success = true,
                message = "TODO: Implement GetTeacherWalletAsync",
                data = new
                {
                    walletId = Guid.Empty,
                    totalBalance = 0,
                    availableBalance = 0,
                    holdBalance = 0
                }
            });
        }

        /// <summary>
        /// [Giáo viên] Xem l?ch s? giao d?ch c?a ví
        /// - Payout: Nh?n ti?n t? l?p h?c
        /// - Withdrawal: Rút ti?n
        /// - Refund: B? tr? ti?n do dispute (n?u có)
        /// 
        /// TODO: Implement GetTeacherTransactionsAsync trong IWalletTransactionService
        /// </summary>
        /// <param name="type">Lo?i giao d?ch (Payout, Withdrawal, Refund, All)</param>
        /// <param name="from">T? ngày</param>
        /// <param name="to">??n ngày</param>
        /// <param name="page">Trang</param>
        /// <param name="pageSize">S? l??ng m?i trang</param>
        [HttpGet("transactions")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetMyTransactions(
            [FromQuery] string? type = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // TODO: Implement
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            return Ok(new
            {
                success = true,
                message = "TODO: Implement GetTeacherTransactionsAsync",
                data = new List<object>(),
                pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalItems = 0,
                    totalPages = 0
                }
            });
        }

        /// <summary>
        /// [Giáo viên] Xem l?ch s? thanh toán t? các l?p h?c
        /// - Thông tin l?p h?c
        /// - S? h?c viên
        /// - T?ng doanh thu
        /// - S? ti?n nh?n ???c (sau tr? phí + dispute)
        /// - Ngày thanh toán
        /// 
        /// TODO: Implement GetTeacherClassPayoutsAsync trong IWalletTransactionService
        /// </summary>
        [HttpGet("payouts")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetMyPayouts(
            [FromQuery] string? status = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // TODO: Implement
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            return Ok(new
            {
                success = true,
                message = "TODO: Implement GetTeacherClassPayoutsAsync",
                data = new List<object>(),
                pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalItems = 0,
                    totalPages = 0
                },
                summary = new
                {
                    totalEarnings = 0,
                    totalPaid = 0,
                    totalPending = 0
                }
            });
        }

        /// <summary>
        /// [Giáo viên] Yêu c?u rút ti?n v? tài kho?n ngân hàng
        /// - S? ti?n ph?i nh? h?n ho?c b?ng AvailableBalance
        /// - C?n có tài kho?n ngân hàng ?ã xác th?c
        /// 
        /// TODO: Implement CreateWithdrawalRequestAsync trong IWalletService
        /// </summary>
        [HttpPost("withdraw")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> RequestWithdrawal([FromBody] WithdrawalRequestDto dto)
        {
            // TODO: Implement
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            return Ok(new
            {
                success = true,
                message = "TODO: Implement CreateWithdrawalRequestAsync"
            });
        }

        /// <summary>
        /// [Giáo viên] Xem danh sách yêu c?u rút ti?n c?a mình
        /// 
        /// TODO: Implement GetTeacherWithdrawalRequestsAsync trong IWalletService
        /// </summary>
        [HttpGet("withdrawal-requests")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetMyWithdrawalRequests(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // TODO: Implement
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            return Ok(new
            {
                success = true,
                message = "TODO: Implement GetTeacherWithdrawalRequestsAsync",
                data = new List<object>(),
                pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalItems = 0,
                    totalPages = 0
                }
            });
        }
    }

    /// <summary>
    /// DTO yêu c?u rút ti?n
    /// </summary>
    public class WithdrawalRequestDto
    {
        /// <summary>
        /// S? ti?n mu?n rút (VND)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// ID tài kho?n ngân hàng ?? nh?n ti?n
        /// </summary>
        public Guid BankAccountId { get; set; }
    }
}
