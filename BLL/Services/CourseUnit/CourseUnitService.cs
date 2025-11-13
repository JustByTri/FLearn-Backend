using BLL.IServices.CourseUnit;
using BLL.IServices.Upload;
using Common.DTO.ApiResponse;
using Common.DTO.CourseUnit.Request;
using Common.DTO.CourseUnit.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.CourseUnits
{
    public class CourseUnitService : ICourseUnitService
    {
        private readonly IUnitOfWork _unit;
        private readonly ICloudinaryService _cloudinaryService;
        public CourseUnitService(IUnitOfWork unit, ICloudinaryService cloudinaryService)
        {
            _unit = unit;
            _cloudinaryService = cloudinaryService;
        }
        public async Task<BaseResponse<UnitResponse>> CreateUnitAsync(Guid userId, Guid courseId, UnitRequest request)
        {

            var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
            if (teacher == null)
                return BaseResponse<UnitResponse>.Fail("Teacher does not exist.");


            var selectedCourse = await _unit.Courses.Query()
                .Include(c => c.CourseUnits)
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.TeacherId == teacher.TeacherId);

            if (selectedCourse == null)
            {
                return BaseResponse<UnitResponse>.Fail("Selected course not found or you are not the owner of this course.");
            }

            if (selectedCourse.Status != CourseStatus.Draft && selectedCourse.Status != CourseStatus.Rejected)
            {
                return BaseResponse<UnitResponse>.Fail("Only Draft or Rejected courses can be updated.");
            }

            int nextPosition = 1;
            if (selectedCourse.CourseUnits.Any())
            {
                nextPosition = selectedCourse.CourseUnits.Max(u => u.Position) + 1;
            }

            var newUnit = new CourseUnit
            {
                CourseUnitID = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description ?? "No description",
                Position = nextPosition,
                CourseID = courseId,
                IsPreview = (request.IsPreview != null) ? request.IsPreview : false,
                CreatedAt = TimeHelper.GetVietnamTime(),
                UpdatedAt = TimeHelper.GetVietnamTime()
            };

            try
            {
                var result = await _unit.CourseUnits.CreateAsync(newUnit);

                if (result < 0)
                {
                    return BaseResponse<UnitResponse>.Fail("Failed to create unit. Please try again later.");
                }

                var response = new UnitResponse
                {
                    CourseUnitID = newUnit.CourseUnitID,
                    Title = request.Title,
                    Description = request.Description,
                    Position = nextPosition,
                    CourseID = selectedCourse.CourseID,
                    CourseTitle = selectedCourse.Title,
                    IsPreview = newUnit.IsPreview,
                    CreatedAt = newUnit.CreatedAt.ToString("dd-MM-yyyy"),
                    UpdatedAt = newUnit.UpdatedAt.ToString("dd-MM-yyyy")
                };

                return BaseResponse<UnitResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return BaseResponse<UnitResponse>.Error($"Error: {ex.Message}");
            }
        }
        public async Task<BaseResponse<object>> DeleteUnitAsync(Guid userId, Guid unitId)
        {
            try
            {
                var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
                if (teacher == null)
                    return BaseResponse<object>.Fail("Teacher does not exist.");

                var unit = await _unit.CourseUnits.Query()
                    .Include(u => u.Course)
                    .Include(u => u.Lessons)
                        .ThenInclude(l => l.Exercises)
                    .FirstOrDefaultAsync(u => u.CourseUnitID == unitId && u.Course.TeacherId == teacher.TeacherId);

                if (unit == null)
                    return BaseResponse<object>.Fail(null, "Unit not found or you don't have permission", 404);

                if (unit.Course.Status != CourseStatus.Draft && unit.Course.Status != CourseStatus.Rejected)
                    return BaseResponse<object>.Fail(null, "Only units in Draft or Rejected courses can be deleted", 400);

                await DeleteUnitMediaAsync(unit);

                unit.Course.NumUnits -= 1;
                unit.Course.NumLessons -= unit.Lessons?.Count ?? 0;

                var remainingUnits = await _unit.CourseUnits.FindAllAsync(
                    u => u.CourseID == unit.CourseID && u.CourseUnitID != unitId);

                foreach (var remainingUnit in remainingUnits.OrderBy(u => u.Position))
                {
                    if (remainingUnit.Position > unit.Position)
                    {
                        remainingUnit.Position--;
                    }
                }

                await _unit.CourseUnits.DeleteAsync(unitId);
                await _unit.SaveChangesAsync();

                return BaseResponse<object>.Success(null, "Unit deleted successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<object>.Error($"Error deleting unit: {ex.Message}");
            }
        }
        public async Task<BaseResponse<UnitResponse>> GetUnitByIdAsync(Guid unitId)
        {
            var unit = await _unit.CourseUnits.Query()
                .OrderBy(u => u.CreatedAt)
                .Include(u => u.Course)
                .FirstOrDefaultAsync(u => u.CourseUnitID == unitId);

            if (unit == null)
                return BaseResponse<UnitResponse>.Fail("CourseUnit not found.");

            return BaseResponse<UnitResponse>.Success(new UnitResponse
            {
                CourseUnitID = unit.CourseUnitID,
                Title = unit.Title,
                Description = unit.Description,
                Position = unit.Position,
                CourseID = unit.CourseID,
                CourseTitle = unit.Course?.Title,
                TotalLessons = unit.TotalLessons ?? 0,
                IsPreview = unit.IsPreview,
                CreatedAt = unit.CreatedAt.ToString("dd-MM-yyyy"),
                UpdatedAt = unit.UpdatedAt.ToString("dd-MM-yyyy")
            });
        }
        public async Task<PagedResponse<IEnumerable<UnitResponse>>> GetUnitsByCourseIdAsync(Guid courseId, PagingRequest request)
        {
            var selectedCourse = await _unit.Courses.GetByIdAsync(courseId);

            if (selectedCourse == null)
            {
                return new PagedResponse<IEnumerable<UnitResponse>>
                {
                    Status = "fail",
                    Code = 404,
                    Message = "Selected course not found.",
                    Data = Enumerable.Empty<UnitResponse>(),
                    Meta = new PagingMeta
                    {
                        Page = request.Page,
                        PageSize = request.PageSize,
                        TotalItems = 0,
                        TotalPages = 0
                    }
                };
            }

            var query = _unit.CourseUnits.Query()
                .OrderBy(u => u.Position)
                .Where(u => u.CourseID == courseId);

            var total = await query.CountAsync();

            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(u => new UnitResponse
                {
                    CourseUnitID = u.CourseUnitID,
                    Title = u.Title,
                    Description = u.Description,
                    IsPreview = u.IsPreview,
                    Position = u.Position,
                    TotalLessons = u.TotalLessons ?? 0,
                    CourseID = selectedCourse.CourseID,
                    CourseTitle = selectedCourse.Title,
                    CreatedAt = selectedCourse.CreatedAt.ToString("dd-MM-yyyy"),
                    UpdatedAt = selectedCourse.UpdatedAt.ToString("dd-MM-yyyy")
                })
                .ToListAsync();

            return PagedResponse<IEnumerable<UnitResponse>>.Success(
                data,
                page: request.Page,
                pageSize: request.PageSize,
                totalItems: total,
                message: "Get units successfully");
        }
        public async Task<BaseResponse<UnitResponse>> UpdateUnitAsync(Guid userId, Guid courseId, Guid unitId, UnitUpdateRequest request)
        {
            var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
            if (teacher == null)
                return BaseResponse<UnitResponse>.Fail("Teacher does not exist.");


            var selectedCourse = await _unit.Courses.Query()
                .Include(c => c.CourseUnits)
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.TeacherId == teacher.TeacherId);

            if (selectedCourse == null)
            {
                return BaseResponse<UnitResponse>.Fail("Selected course not found or you are not the owner of this course.");
            }

            if (selectedCourse.Status != CourseStatus.Draft && selectedCourse.Status != CourseStatus.Rejected)
            {
                return BaseResponse<UnitResponse>.Fail("Only Draft or Rejected courses can be updated.");
            }

            var unit = await _unit.CourseUnits.GetByIdAsync(unitId);
            if (unit == null)
                return BaseResponse<UnitResponse>.Fail(null, "CourseUnit not found.", 404);

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                unit.Title = request.Title.Trim().Length > 200
                    ? request.Title.Trim().Substring(0, 200)
                    : request.Title.Trim();
            }

            if (request.IsPreview.HasValue)
            {
                unit.IsPreview = request.IsPreview.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                unit.Description = request.Description.Trim().Length > 500
                    ? request.Description.Trim().Substring(0, 500)
                    : request.Description.Trim();
            }

            unit.UpdatedAt = TimeHelper.GetVietnamTime();

            try
            {
                var result = await _unit.SaveChangesAsync();

                if (result < 0)
                {
                    return BaseResponse<UnitResponse>.Fail($"Failed when updating unit with id: {unitId}");
                }

                return BaseResponse<UnitResponse>.Success(new UnitResponse
                {
                    CourseUnitID = unit.CourseUnitID,
                    Title = unit.Title,
                    Description = unit.Description,
                    Position = unit.Position,
                    CourseID = selectedCourse.CourseID,
                    CourseTitle = selectedCourse.Title,
                    TotalLessons = unit.TotalLessons ?? 0,
                    CreatedAt = unit.CreatedAt.ToString("dd-MM-yyyy"),
                    UpdatedAt = unit.UpdatedAt.ToString("dd-MM-yyyy"),
                    IsPreview = unit.IsPreview,
                });
            }
            catch (Exception ex)
            {
                return BaseResponse<UnitResponse>.Error($"Error: {ex.Message}");
            }

        }
        #region
        private async Task DeleteUnitMediaAsync(CourseUnit unit)
        {
            foreach (var lesson in unit.Lessons ?? Enumerable.Empty<DAL.Models.Lesson>())
            {
                if (!string.IsNullOrEmpty(lesson.VideoPublicId))
                {
                    await _cloudinaryService.DeleteFileAsync(lesson.VideoPublicId);
                }
                if (!string.IsNullOrEmpty(lesson.DocumentPublicId))
                {
                    await _cloudinaryService.DeleteFileAsync(lesson.DocumentPublicId);
                }

                foreach (var exercise in lesson.Exercises ?? Enumerable.Empty<DAL.Models.Exercise>())
                {
                    if (!string.IsNullOrEmpty(exercise.MediaPublicId))
                    {
                        await _cloudinaryService.DeleteFileAsync(exercise.MediaPublicId);
                    }
                }
            }
        }
        #endregion
    }
}
