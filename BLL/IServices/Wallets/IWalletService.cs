namespace BLL.IServices.Wallets
{
    public interface IWalletService
    {
        Task TransferToAdminWalletAsync(Guid purchaseId);
        Task TransferToTeacherWalletAsync(Guid purchaseId);
        Task TransferExerciseGradingFeeToTeacherAsync(Guid allocationId);
        Task ProcessCourseCreationFeeTransferAsync(Guid purchaseId);
    }
}
