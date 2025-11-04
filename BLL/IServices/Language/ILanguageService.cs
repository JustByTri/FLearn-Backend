using Common.DTO.ApiResponse;
using Common.DTO.Language.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.Language
{
    public interface ILanguageService
    {
        Task<BaseResponse<IEnumerable<LanguageResponse>>> GetAllAsync();
        Task<IEnumerable<LanguageLevelDto>> GetLanguageLevelsAsync(Guid languageId);
        Task<PagedResponse<IEnumerable<ProgramResponse>>> GetProgramResponsesAsync(string langCode, PagingRequest pagingRequest);
    }
}
