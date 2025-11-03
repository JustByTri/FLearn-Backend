using BLL.IServices.Language;
using Common.DTO.ApiResponse;
using Common.DTO.Language.Response;
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
    }
}

