using Common.DTO.Teacher.Response;
namespace Common.DTO.Teacher
{
    public class TeacherProfileWithWalletResponse
    {
        public TeacherProfileResponse? Profile { get; set; }
        public TeacherWalletDto? Wallet { get; set; }
    }
    public class TeacherWalletDto
    {
        public Guid WalletId { get; set; }
        public decimal? TotalBalance { get; set; }
        public decimal? AvailableBalance { get; set; }
        public decimal? HoldBalance { get; set; }
        public string Currency { get; set; } = "VND";
    }
}
