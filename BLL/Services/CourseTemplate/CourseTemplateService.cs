using BLL.IServices.CourseTemplate;
using Common.DTO.ApiResponse;
using Common.DTO.CourseTemplate.Request;
using Common.DTO.CourseTemplate.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.Helpers;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.CourseTemplate
{
    public class CourseTemplateService : ICourseTemplateService
    {
        private readonly IUnitOfWork _unit;
        private const string DefaultScoringCriteriaJson = "{\r\n  \"SpeakingScoringCriteria\": {\r\n    \"FluencyAndCoherence\": {\r\n      \"description\": \"Ability to speak smoothly, maintain logical flow, and use cohesive devices appropriately. Includes handling of pauses, hesitations, and discourse markers.\",\r\n      \"scale\": \"0-9\",\r\n      \"weight\": 0.25\r\n    },\r\n    \"LexicalResource\": {\r\n      \"description\": \"Range, precision, and appropriacy of vocabulary. Covers ability to paraphrase, use idiomatic expressions, and adapt vocabulary to context.\",\r\n      \"scale\": \"0-9\",\r\n      \"weight\": 0.25\r\n    },\r\n    \"GrammaticalRangeAndAccuracy\": {\r\n      \"description\": \"Variety and accuracy of grammatical structures. Includes control of complex sentences, verb tenses, agreement, and word order.\",\r\n      \"scale\": \"0-9\",\r\n      \"weight\": 0.25\r\n    },\r\n    \"Pronunciation\": {\r\n      \"description\": \"Clarity, stress, rhythm, and intonation. Assesses comprehensibility and ability to use features of connected speech effectively.\",\r\n      \"scale\": \"0-9\",\r\n      \"weight\": 0.25\r\n    }\r\n  }\r\n}\r\n";
        public CourseTemplateService(IUnitOfWork unit)
        {
            _unit = unit;
        }
        public async Task<BaseResponse<CourseTemplateResponse>> CreateAsync(CourseTemplateRequest request)
        {

            try
            {
                var program = await _unit.Programs.Query()
                    .Include(p => p.Levels)
                    .OrderBy(p => p.CreatedAt)
                    .Where(p => p.ProgramId == request.ProgramId && p.Status == true)
                    .FirstOrDefaultAsync();

                if (program == null)
                    return BaseResponse<CourseTemplateResponse>.Fail(new { ProgramId = "Not found" }, "Program not found", 404);

                var level = program.Levels
                    .Where(l => l.LevelId == request.LevelId && l.Status == true)
                    .FirstOrDefault();

                if (level == null)
                    return BaseResponse<CourseTemplateResponse>.Fail(new { LevelId = "Not found" }, "Level not found in the specified program", 404);

                var newTemplate = new DAL.Models.CourseTemplate
                {
                    TemplateId = Guid.NewGuid(),
                    ProgramId = program.ProgramId,
                    LevelId = level.LevelId,
                    ScoringCriteriaJson = DefaultScoringCriteriaJson,
                    Name = string.IsNullOrWhiteSpace(request.Name) ? "Untitled Course Template" : request.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(request.Description) ? string.Empty : request.Description.Trim(),
                    UnitCount = request.UnitCount < 0 ? 5 : request.UnitCount,
                    Version = "1.0.0",
                    LessonsPerUnit = request.LessonsPerUnit < 0 ? 5 : request.LessonsPerUnit,
                    ExercisesPerLesson = request.ExercisesPerLesson < 0 ? 5 : request.ExercisesPerLesson,
                };

                await _unit.CourseTemplates.CreateAsync(newTemplate);

                var response = new CourseTemplateResponse
                {
                    TemplateId = newTemplate.TemplateId,
                    Name = newTemplate.Name,
                    Description = newTemplate.Description,
                    UnitCount = newTemplate.UnitCount,
                    LessonsPerUnit = newTemplate.LessonsPerUnit,
                    ExercisesPerLesson = newTemplate.ExercisesPerLesson,
                    ScoringCriteriaJson = newTemplate.ScoringCriteriaJson,
                    Level = level.Name,
                    Program = program.Name,
                    Version = newTemplate.Version,
                    CreatedAt = newTemplate.CreatedAt.ToString("dd-MM-yyyy"),
                    ModifiedAt = newTemplate.ModifiedAt?.ToString("dd-MM-yyyy")
                };

                return BaseResponse<CourseTemplateResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return BaseResponse<CourseTemplateResponse>.Error(ex.Message);
            }
        }
        public async Task<PagedResponse<IEnumerable<CourseTemplateResponse>>> GetAllAsync(PagingRequest request)
        {
            var query = _unit.CourseTemplates.Query()
                .Include(t => t.Program)
                .Include(t => t.Level)
                .Where(t => t.Status == true);

            var totalItems = await query.CountAsync();

            var templates = await query
                .OrderBy(t => t.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new CourseTemplateResponse
                {
                    TemplateId = t.TemplateId,
                    Name = t.Name,
                    Description = t.Description,
                    UnitCount = t.UnitCount,
                    LessonsPerUnit = t.LessonsPerUnit,
                    ExercisesPerLesson = t.ExercisesPerLesson,
                    ScoringCriteriaJson = t.ScoringCriteriaJson,
                    Level = t.Level != null ? t.Level.Name : string.Empty,
                    Program = t.Program != null ? t.Program.Name : string.Empty,
                    Version = t.Version,
                    CreatedAt = t.CreatedAt.ToString("dd-MM-yyyy"),
                    ModifiedAt = t.ModifiedAt.HasValue ? t.ModifiedAt.Value.ToString("dd-MM-yyyy") : null
                })
                .ToListAsync();

            if (templates == null || !templates.Any())
            {
                return PagedResponse<IEnumerable<CourseTemplateResponse>>.Success(
                    new List<CourseTemplateResponse>(),
                    request.Page,
                    request.PageSize,
                    totalItems,
                    "No templates found"
                );
            }

            return PagedResponse<IEnumerable<CourseTemplateResponse>>.Success(
                templates,
                request.Page,
                request.PageSize,
                totalItems,
                "Fetched templates successfully"
            );
        }

        public async Task<BaseResponse<CourseTemplateResponse>> GetByIdAsync(Guid id)
        {
            var selectedTemplate = await _unit.CourseTemplates.Query()
                .Include(t => t.Program)
                .Include(t => t.Level)
                .FirstOrDefaultAsync(t => t.TemplateId == id && t.Status == true);

            if (selectedTemplate == null)
            {
                return BaseResponse<CourseTemplateResponse>.Fail(new { Id = "Not found" }, "CourseTemplate not found", 404);
            }

            var response = new CourseTemplateResponse
            {
                TemplateId = selectedTemplate.TemplateId,
                Name = selectedTemplate.Name,
                Description = selectedTemplate.Description,
                UnitCount = selectedTemplate.UnitCount,
                LessonsPerUnit = selectedTemplate.LessonsPerUnit,
                ExercisesPerLesson = selectedTemplate.ExercisesPerLesson,
                ScoringCriteriaJson = selectedTemplate.ScoringCriteriaJson,
                Level = selectedTemplate.Level?.Name ?? string.Empty,
                Program = selectedTemplate.Program?.Name ?? string.Empty,
                Version = selectedTemplate.Version,
                CreatedAt = selectedTemplate.CreatedAt.ToString("dd-MM-yyyy"),
                ModifiedAt = selectedTemplate.ModifiedAt?.ToString("dd-MM-yyyy")
            };

            return BaseResponse<CourseTemplateResponse>.Success(
                response,
                "CourseTemplate retrieved successfully"
            );
        }
        public async Task<BaseResponse<CourseTemplateResponse>> UpdateAsync(Guid id, CourseTemplateRequest request)
        {
            if (request == null)
                return BaseResponse<CourseTemplateResponse>.Error("Invalid request.");

            var selectedTemplate = await _unit.CourseTemplates.Query()
                .Include(t => t.Program)
                .Include(t => t.Level)
                .OrderBy(t => t.CreatedAt)
                .FirstOrDefaultAsync(t => t.TemplateId == id && t.Status == true);

            if (selectedTemplate == null)
            {
                return BaseResponse<CourseTemplateResponse>.Fail(new { Id = "Not found" }, "CourseTemplate not found", 404);
            }

            try
            {
                if (request.ProgramId != Guid.Empty && request.ProgramId != selectedTemplate.ProgramId)
                {
                    var program = await _unit.Programs.Query()
                        .Include(p => p.Levels)
                        .FirstOrDefaultAsync(p => p.ProgramId == request.ProgramId && p.Status == true);

                    if (program == null)
                        return BaseResponse<CourseTemplateResponse>.Fail(new { ProgramId = "Not found" }, "Program not found", 404);

                    if (request.LevelId != Guid.Empty)
                    {
                        var level = program.Levels.FirstOrDefault(l => l.LevelId == request.LevelId && l.Status == true);
                        if (level == null)
                            return BaseResponse<CourseTemplateResponse>.Fail(new { LevelId = "Not found" }, "Level not found in the specified program", 404);

                        selectedTemplate.LevelId = level.LevelId;
                    }

                    selectedTemplate.ProgramId = program.ProgramId;
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                    selectedTemplate.Name = request.Name.Trim();

                if (request.Description != null)
                    selectedTemplate.Description = string.IsNullOrWhiteSpace(request.Description)
                        ? selectedTemplate.Description
                        : request.Description.Trim();

                selectedTemplate.UnitCount = request.UnitCount < 0 ? 5 : request.UnitCount;
                selectedTemplate.LessonsPerUnit = request.LessonsPerUnit < 0 ? 5 : request.LessonsPerUnit;
                selectedTemplate.ExercisesPerLesson = request.ExercisesPerLesson < 0 ? 5 : request.ExercisesPerLesson;

                selectedTemplate.ModifiedAt = TimeHelper.GetVietnamTime();

                await _unit.SaveChangesAsync();

                var response = new CourseTemplateResponse
                {
                    TemplateId = selectedTemplate.TemplateId,
                    Name = selectedTemplate.Name,
                    Description = selectedTemplate.Description,
                    UnitCount = selectedTemplate.UnitCount,
                    LessonsPerUnit = selectedTemplate.LessonsPerUnit,
                    ExercisesPerLesson = selectedTemplate.ExercisesPerLesson,
                    ScoringCriteriaJson = selectedTemplate.ScoringCriteriaJson,
                    Level = selectedTemplate.Level?.Name ?? string.Empty,
                    Program = selectedTemplate.Program?.Name ?? string.Empty,
                    Version = selectedTemplate.Version,
                    CreatedAt = selectedTemplate.CreatedAt.ToString("dd-MM-yyyy"),
                    ModifiedAt = selectedTemplate.ModifiedAt?.ToString("dd-MM-yyyy")
                };

                return BaseResponse<CourseTemplateResponse>.Success(
                    response,
                    "CourseTemplate updated successfully"
                );
            }
            catch (DbUpdateException ex)
            {
                return BaseResponse<CourseTemplateResponse>.Error($"Database error while updating course template. \\ {ex.Message}");
            }
            catch (Exception ex)
            {
                return BaseResponse<CourseTemplateResponse>.Error($"Unexpected error while updating course template. \\ {ex.Message}");
            }
        }
    }
}
