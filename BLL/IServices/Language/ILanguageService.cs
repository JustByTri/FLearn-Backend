using Common.DTO.ApiResponse;
using Common.DTO.Language.Response;
using Common.DTO.Leaderboard;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.Language
{
    public interface ILanguageService
    {
        Task<BaseResponse<IEnumerable<LanguageResponse>>> GetAllAsync();
        Task<IEnumerable<LanguageLevelDto>> GetLanguageLevelsAsync(Guid languageId);
        Task<PagedResponse<IEnumerable<ProgramResponse>>> GetProgramResponsesAsync(string langCode, PagingRequest pagingRequest);
        /// <summary>
        /// Leaderboard by language
        /// </summary>
        /// <param name="languageId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        Task<BaseResponse<IEnumerable<LeaderboardEntryDto>>> GetLeaderboardByLanguageAsync(Guid languageId, int count = 20);

        Task<BaseResponse<MyRankDto>> GetMyRankAsync(Guid languageId, Guid userId);
    }
}
