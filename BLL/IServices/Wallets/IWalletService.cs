namespace BLL.IServices.Wallets
{
    public interface IWalletService
    {
        Task TransferToAdminWalletAsync(Guid purchaseId);
        Task TransferToTeacherWalletAsync(Guid purchaseId);
    }
}
