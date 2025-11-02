//using BLL.IServices.CourseTemplate;
//using Common.DTO.ApiResponse;
//using Common.DTO.CourseTemplate.Request;
//using Common.DTO.CourseTemplate.Response;
//using Common.DTO.Paging.Request;
//using Common.DTO.Paging.Response;
//using DAL.Helpers;
//using DAL.UnitOfWork;
//using Microsoft.EntityFrameworkCore;

//namespace BLL.Services.CourseTemplate
//{
//    public class CourseTemplateService : ICourseTemplateService
//    {
//        private readonly IUnitOfWork _unit;
//        public CourseTemplateService(IUnitOfWork unit)
//        {
//            _unit = unit;
//        }
//        public async Task<BaseResponse<CourseTemplateResponse>> CreateAsync(CourseTemplateRequest request)
//        {
//            if (request == null)
//                return BaseResponse<CourseTemplateResponse>.Error("Invalid request.");

//            try
//            {
//                var newTemplate = new DAL.Models.CourseTemplate
//                {
//                    Id = Guid.NewGuid(),
//                    Name = string.IsNullOrWhiteSpace(request.Name) ? "Untitled Course Template" : request.Name.Trim(),
//                    Description = string.IsNullOrWhiteSpace(request.Description) ? string.Empty : request.Description.Trim(),
//                    RequireGoal = request.RequireGoal,
//                    RequireTopic = request.RequireTopic,
//                    RequireLang = request.RequireLang,
//                    RequireLevel = request.RequireLevel,
//                    MinUnits = request.MinUnits < 0 ? 5 : request.MinUnits,
//                    MinLessonsPerUnit = request.MinLessonsPerUnit < 0 ? 5 : request.MinLessonsPerUnit,
//                    MinExercisesPerLesson = request.MinExercisesPerLesson < 0 ? 5 : request.MinExercisesPerLesson,
//                    CreatedAt = TimeHelper.GetVietnamTime(),
//                };

//                await _unit.CourseTemplates.CreateAsync(newTemplate);

//                var response = new CourseTemplateResponse
//                {
//                    Id = newTemplate.Id,
//                    Name = newTemplate.Name,
//                    Description = newTemplate.Description,
//                    RequireGoal = newTemplate.RequireGoal,
//                    RequireTopic = newTemplate.RequireTopic,
//                    RequireLang = newTemplate.RequireLang,
//                    RequireLevel = newTemplate.RequireLevel,
//                    MinUnits = newTemplate.MinUnits,
//                    MinLessonsPerUnit = newTemplate.MinLessonsPerUnit,
//                    MinExercisesPerLesson = newTemplate.MinExercisesPerLesson,
//                };

//                return BaseResponse<CourseTemplateResponse>.Success(response);
//            }
//            catch (Exception ex)
//            {
//                return BaseResponse<CourseTemplateResponse>.Error(ex.Message);
//            }
//        }
//        public async Task<PagedResponse<IEnumerable<CourseTemplateResponse>>> GetAllAsync(PagingRequest request)
//        {
//            var query = _unit.CourseTemplates.Query().Where(t => t.Status == true);

//            var totalItems = await query.CountAsync();

//            var templates = await query
//                .OrderBy(t => t.CreatedAt)
//                .Skip((request.Page - 1) * request.PageSize)
//                .Take(request.PageSize)
//                .Select(t => new CourseTemplateResponse
//                {
//                    Id = t.Id,
//                    Name = t.Name,
//                    Description = t.Description,
//                    RequireGoal = t.RequireGoal,
//                    RequireLevel = t.RequireLevel,
//                    RequireTopic = t.RequireTopic,
//                    RequireLang = t.RequireLang,
//                    MinUnits = t.MinUnits,
//                    MinLessonsPerUnit = t.MinLessonsPerUnit,
//                    MinExercisesPerLesson = t.MinExercisesPerLesson
//                })
//                .ToListAsync();

//            if (templates == null || !templates.Any())
//            {
//                return PagedResponse<IEnumerable<CourseTemplateResponse>>.Success(
//                    new List<CourseTemplateResponse>(),
//                    request.Page,
//                    request.PageSize,
//                    totalItems,
//                    "No templates found"
//                );
//            }

//            return PagedResponse<IEnumerable<CourseTemplateResponse>>.Success(
//                templates,
//                request.Page,
//                request.PageSize,
//                totalItems,
//                "Fetched templates successfully"
//            );
//        }

//        public async Task<BaseResponse<CourseTemplateResponse>> GetByIdAsync(Guid id)
//        {
//            var selectedTemplate = await _unit.CourseTemplates.GetByIdAsync(id);

//            if (selectedTemplate == null)
//            {
//                return BaseResponse<CourseTemplateResponse>.Fail(new { Id = "Not found" }, "CourseTemplate not found", 404);
//            }

//            var response = new CourseTemplateResponse
//            {
//                Id = selectedTemplate.Id,
//                Name = selectedTemplate.Name,
//                Description = selectedTemplate.Description,
//                RequireGoal = selectedTemplate.RequireGoal,
//                RequireLevel = selectedTemplate.RequireLevel,
//                RequireTopic = selectedTemplate.RequireTopic,
//                RequireLang = selectedTemplate.RequireLang,
//                MinUnits = selectedTemplate.MinUnits,
//                MinLessonsPerUnit = selectedTemplate.MinLessonsPerUnit,
//                MinExercisesPerLesson = selectedTemplate.MinExercisesPerLesson
//            };

//            return BaseResponse<CourseTemplateResponse>.Success(
//               response,
//               "CourseTemplate retrieved successfully"
//           );
//        }

//        public async Task<BaseResponse<CourseTemplateResponse>> UpdateAsync(Guid id, CourseTemplateRequest request)
//        {
//            if (request == null)
//                return BaseResponse<CourseTemplateResponse>.Error("Invalid request.");

//            var selectedTemplate = await _unit.CourseTemplates.GetByIdAsync(id);

//            if (selectedTemplate == null)
//            {
//                return BaseResponse<CourseTemplateResponse>.Fail(new { Id = "Not found" }, "CourseTemplate not found", 404);
//            }

//            try
//            {
//                if (!string.IsNullOrWhiteSpace(request.Name))
//                    selectedTemplate.Name = request.Name.Trim();
//                if (request.Description != null)
//                    selectedTemplate.Description = string.IsNullOrWhiteSpace(request.Description)
//                        ? selectedTemplate.Description
//                        : request.Description.Trim();

//                selectedTemplate.RequireGoal = request.RequireGoal;
//                selectedTemplate.RequireLevel = request.RequireLevel;
//                selectedTemplate.RequireTopic = request.RequireTopic;
//                selectedTemplate.RequireLang = request.RequireLang;

//                if (request.MinUnits > 0)
//                    selectedTemplate.MinUnits = request.MinUnits;

//                if (request.MinLessonsPerUnit > 0)
//                    selectedTemplate.MinLessonsPerUnit = request.MinLessonsPerUnit;

//                if (request.MinExercisesPerLesson > 0)
//                    selectedTemplate.MinExercisesPerLesson = request.MinExercisesPerLesson;

//                selectedTemplate.ModifiedAt = TimeHelper.GetVietnamTime();

//                await _unit.SaveChangesAsync();

//                var response = new CourseTemplateResponse
//                {
//                    Id = selectedTemplate.Id,
//                    Name = selectedTemplate.Name,
//                    Description = selectedTemplate.Description,
//                    RequireGoal = selectedTemplate.RequireGoal,
//                    RequireLevel = selectedTemplate.RequireLevel,
//                    RequireTopic = selectedTemplate.RequireTopic,
//                    RequireLang = selectedTemplate.RequireLang,
//                    MinUnits = selectedTemplate.MinUnits,
//                    MinLessonsPerUnit = selectedTemplate.MinLessonsPerUnit,
//                    MinExercisesPerLesson = selectedTemplate.MinExercisesPerLesson
//                };

//                return BaseResponse<CourseTemplateResponse>.Success(
//                    response,
//                    "CourseTemplate updated successfully"
//                );
//            }
//            catch (DbUpdateException)
//            {
//                return BaseResponse<CourseTemplateResponse>.Error("Database error while updating course template.");
//            }
//            catch (Exception)
//            {
//                return BaseResponse<CourseTemplateResponse>.Error("Unexpected error while updating course template.");
//            }
//        }
//    }
//}
