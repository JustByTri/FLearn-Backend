using BLL.IServices.Language;
using Common.DTO.ApiResponse;
using Common.DTO.Language.Response;
using Common.DTO.Leaderboard;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace BLL.Services.Languages
{
    public class LanguageService : ILanguageService
    {
        private readonly IUnitOfWork _unit;
        public LanguageService(IUnitOfWork unit)
        {
            _unit = unit;
        }

        public async Task<BaseResponse<IEnumerable<LanguageResponse>>> GetAllAsync()
        {
            var languages = await _unit.Languages.Query()
                .OrderBy(l => l.LanguageCode)
                .Select(l => new LanguageResponse
                {
                    Id = l.LanguageID,
                    LangName = l.LanguageName,
                    LangCode = l.LanguageCode,
                })
                .ToListAsync();

            return BaseResponse<IEnumerable<LanguageResponse>>.Success(languages);
        }
        public async Task<IEnumerable<LanguageLevelDto>> GetLanguageLevelsAsync(Guid languageId)
        {

            var language = await _unit.Languages.GetByIdAsync(languageId);
            if (language == null)
            {

                throw new KeyNotFoundException("Không tìm thấy ngôn ngữ này.");
            }


            var allLevels = await _unit.LanguageLevels.GetAllAsync();


            var levels = allLevels
                .Where(ll => ll.LanguageID == languageId)
                .OrderBy(ll => ll.OrderIndex)
                .Select(ll => new LanguageLevelDto
                {
                    LanguageLevelID = ll.LanguageLevelID,
                    LevelName = ll.LevelName,
                    Description = ll.Description ?? "",
                    OrderIndex = ll.OrderIndex
                })
                .ToList();

            return levels;
        }

        public async Task<PagedResponse<IEnumerable<ProgramResponse>>> GetProgramResponsesAsync(string langCode, PagingRequest pagingRequest)
        {
            try
            {
                var language = await _unit.Languages.FindByLanguageCodeAsync(langCode);
                if (language == null)
                    return PagedResponse<IEnumerable<ProgramResponse>>.Fail(
                        errors: null,
                        message: "Language not found",
                        code: 404);

                var query = _unit.Programs.Query();
                var totalItems = await query.CountAsync();
                var programs = await query
                    .Where(p => p.LanguageId == language.LanguageID)
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((pagingRequest.Page - 1) * pagingRequest.PageSize)
                    .Take(pagingRequest.PageSize)
                    .Include(p => p.Levels)
                    .ToListAsync();

                var programResponses = programs.Select(p => new ProgramResponse
                {
                    ProgramId = p.ProgramId,
                    ProgramName = p.Name,
                    Description = p.Description,
                    Levels = p.Levels.Select(l => new LevelResponse
                    {
                        LevelId = l.LevelId,
                        LevelName = l.Name,
                        Description = l.Description
                    }).ToList()
                });

                return PagedResponse<IEnumerable<ProgramResponse>>.Success(
                    data: programResponses,
                    page: pagingRequest.Page,
                    pageSize: pagingRequest.PageSize,
                    totalItems: totalItems);
            }
            catch (Exception ex)
            {
                return PagedResponse<IEnumerable<ProgramResponse>>.Error(
                    message: "An error occurred while retrieving programs.",
                    code: 500,
                    errors: new { Exception = ex.Message });
            }
        }
        public async Task<BaseResponse<IEnumerable<LeaderboardEntryDto>>> GetLeaderboardByLanguageAsync(Guid languageId, int count = 20)
        {
            var language = await _unit.Languages.GetByIdAsync(languageId);
            if (language == null)
            {
                return BaseResponse<IEnumerable<LeaderboardEntryDto>>.Fail(null, "Không tìm thấy ngôn ngữ.", (int)HttpStatusCode.NotFound);
            }

            var learners = await _unit.LearnerLanguages.GetLeaderboardAsync(languageId, count);

            int rank = 1;
            var leaderboard = learners.Select(ll => new LeaderboardEntryDto
            {
                Rank = rank++,
                LearnerId = ll.UserId,
                FullName = ll.User.FullName ?? "N/A",
                Avatar = ll.User.Avatar,
                StreakDays = ll.StreakDays, 
                Level = ll.ProficiencyLevel 
            });

            return BaseResponse<IEnumerable<LeaderboardEntryDto>>.Success(
                leaderboard,
                $"Lấy bảng xếp hạng cho ngôn ngữ {language.LanguageName} thành công.",
                (int)HttpStatusCode.OK
            );
        }

        public async Task<BaseResponse<MyRankDto>> GetMyRankAsync(Guid languageId, Guid userId)
        {
            var learnerLangs = await _unit.LearnerLanguages
                .GetByConditionAsync(ll => ll.UserId == userId && ll.LanguageId == languageId);
            var learnerLang = learnerLangs.FirstOrDefault();

            if (learnerLang == null)
            {
                return BaseResponse<MyRankDto>.Fail(null, "Bạn chưa bắt đầu học ngôn ngữ này.", (int)HttpStatusCode.NotFound);
            }

            int currentStreak = learnerLang.StreakDays; 
            int rank = await _unit.LearnerLanguages.GetRankAsync(languageId, currentStreak);

            var myRankDto = new MyRankDto
            {
                LanguageId = languageId,
                StreakDays = currentStreak,
                Rank = rank,
                Level = learnerLang.ProficiencyLevel
            };

            return BaseResponse<MyRankDto>.Success(
                myRankDto,
                "Lấy thông tin xếp hạng thành công.",
                (int)HttpStatusCode.OK
            );
        }
    }
}

