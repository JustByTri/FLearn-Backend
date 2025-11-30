namespace BLL.IServices.AI
{
    public interface IAIContentModerationService
    {
        Task<bool> IsContentSafeAsync(string textContent);
    }
}
