using BLL.IServices.Language;
using Common.DTO.ApiResponse;
using Common.DTO.Language.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

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
    }
}

