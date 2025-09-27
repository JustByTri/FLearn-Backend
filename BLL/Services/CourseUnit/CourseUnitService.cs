using BLL.IServices.CourseUnit;
using Common.DTO.ApiResponse;
using Common.DTO.CourseUnit.Request;
using Common.DTO.CourseUnit.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.CourseUnits
{
    public class CourseUnitService : ICourseUnitService
    {
        private readonly IUnitOfWork _unit;
        public CourseUnitService(IUnitOfWork unit)
        {
            _unit = unit;
        }
        public async Task<BaseResponse<UnitResponse>> CreateUnitAsync(Guid teacherId, Guid courseId, UnitRequest request)
        {
            var teacher = await _unit.Users.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == teacherId);

            if (teacher == null || !teacher.UserRoles.Any(ur => ur.Role.Name == "Teacher"))
            {
                return BaseResponse<UnitResponse>.Fail("Invalid TeacherID. Teacher not found or does not have role 'Teacher'.");
            }

            var selectedCourse = await _unit.Courses.Query()
                .Include(c => c.CourseUnits)
                .FirstOrDefaultAsync(c => c.CourseID == courseId);

            if (selectedCourse == null)
            {
                return BaseResponse<UnitResponse>.Fail("Selected course not found.");
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
                Description = request.Description,
                Position = nextPosition,
                CourseID = courseId,
                IsPreview = (request.IsPreview != null) ? request.IsPreview : false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                    CreatedAt = newUnit.CreatedAt,
                    UpdatedAt = newUnit.UpdatedAt,
                };

                return BaseResponse<UnitResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return BaseResponse<UnitResponse>.Error($"Error: {ex.Message}");
            }
        }

        public async Task<BaseResponse<UnitResponse>> GetUnitByIdAsync(Guid unitId)
        {
            var unit = await _unit.CourseUnits.Query()
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
                CourseTitle = unit.Course.Title,
                TotalLessons = unit.TotalLessons ?? 0,
                IsPreview = unit.IsPreview,
                CreatedAt = unit.CreatedAt,
                UpdatedAt = unit.UpdatedAt,
            });
        }

        public async Task<PagedResponse<IEnumerable<UnitResponse>>> GetUnitsAsync(PagingRequest request)
        {
            var query = _unit.CourseUnits.Query()
                .OrderBy(u => u.CreatedAt);

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
                    CourseID = u.CourseID,
                    CourseTitle = u.Course.Title ?? "None",
                    TotalLessons = u.TotalLessons ?? 0,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                })
                .ToListAsync();

            return PagedResponse<IEnumerable<UnitResponse>>.Success(data, total, request.Page, request.PageSize);
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
                .Where(u => u.CourseID == courseId)
                .OrderBy(u => u.Position);

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
                    CourseID = selectedCourse.CourseID,
                    CourseTitle = selectedCourse.Title,
                    CreatedAt = selectedCourse.CreatedAt,
                    UpdatedAt = selectedCourse.UpdatedAt,
                })
                .ToListAsync();

            return PagedResponse<IEnumerable<UnitResponse>>.Success(
                data,
                page: request.Page,
                pageSize: request.PageSize,
                totalItems: total,
                message: "Get units successfully");
        }

        public async Task<BaseResponse<UnitResponse>> UpdateUnitAsync(Guid teacherId, Guid courseId, Guid unitId, UnitRequest request)
        {
            var teacher = await _unit.Users.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == teacherId);

            if (teacher == null || !teacher.UserRoles.Any(ur => ur.Role.Name == "Teacher"))
            {
                return BaseResponse<UnitResponse>.Fail("Invalid TeacherID. Teacher not found or does not have role 'Teacher'.");
            }

            var selectedCourse = await _unit.Courses.Query()
                .Include(c => c.CourseUnits)
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.TeacherID == teacherId);

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
                return BaseResponse<UnitResponse>.Fail("CourseUnit not found.");

            unit.Title = request.Title;
            unit.IsPreview = request.IsPreview;
            unit.Description = request.Description;
            unit.UpdatedAt = DateTime.UtcNow;

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
                    CreatedAt = unit.CreatedAt,
                    UpdatedAt = unit.UpdatedAt,
                    IsPreview = unit.IsPreview,
                });
            }
            catch (Exception ex)
            {
                return BaseResponse<UnitResponse>.Error($"Error: {ex.Message}");
            }

        }
    }
}
