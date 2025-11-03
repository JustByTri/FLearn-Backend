using Common.DTO.ApiResponse;
using Common.DTO.Language.Response;

namespace BLL.IServices.Language
{
    public interface ILanguageService
    {
        Task<BaseResponse<IEnumerable<LanguageResponse>>> GetAllAsync();
        Task<IEnumerable<LanguageLevelDto>> GetLanguageLevelsAsync(Guid languageId);
    }
}
