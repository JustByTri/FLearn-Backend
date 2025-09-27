using BLL.IServices.Lesson;
using BLL.IServices.Upload;
using Common.DTO.ApiResponse;
using Common.DTO.Lesson.Request;
using Common.DTO.Lesson.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Lessons
{
    public class LessonService : ILessonService
    {
        private readonly IUnitOfWork _unit;
        private readonly ICloudinaryService _cloudinary;
        public LessonService(IUnitOfWork unit, ICloudinaryService cloudinary)
        {
            _unit = unit;
            _cloudinary = cloudinary;
        }
        public async Task<BaseResponse<LessonResponse>> CreateLessonAsync(Guid teacherId, Guid courseId, Guid unitId, LessonRequest request)
        {

            var teacher = await _unit.Users.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == teacherId);

            if (teacher == null || !teacher.UserRoles.Any(ur => ur.Role.Name == "Teacher"))
            {
                return BaseResponse<LessonResponse>.Fail("Invalid TeacherID. Teacher not found or does not have role 'Teacher'.");
            }

            var selectedCourse = await _unit.Courses.Query()
                .Include(c => c.CourseUnits)
                .FirstOrDefaultAsync(c => c.CourseID == courseId);

            if (selectedCourse == null)
            {
                return BaseResponse<LessonResponse>.Fail("Selected course not found.");
            }

            if (selectedCourse.Status != CourseStatus.Draft && selectedCourse.Status != CourseStatus.Rejected)
            {
                return BaseResponse<LessonResponse>.Fail("Only Draft or Rejected courses can be updated.");
            }


            var selectedUnit = await _unit.CourseUnits.Query()
                .Include(u => u.Lessons)
                .Where(u => u.CourseID == courseId && u.CourseUnitID == unitId)
                .FirstOrDefaultAsync();

            if (selectedUnit == null)
                return BaseResponse<LessonResponse>.Fail("Unit not found or not in course");

            string? videoUrl = null;
            string? videoPublicId = null;
            string? docUrl = null;
            string? docPublicId = null;

            try
            {
                if (request.VideoFile != null)
                {
                    var video = await _cloudinary.UploadVideoAsync(request.VideoFile, "lessons/videos");
                    videoUrl = video.Url;
                    videoPublicId = video.PublicId;
                }

                if (request.DocumentFile != null)
                {
                    var doc = await _cloudinary.UploadDocumentAsync(request.DocumentFile, "lessons/documents");
                    docUrl = doc.Url;
                    docPublicId = doc.PublicId;
                }

            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(videoPublicId))
                    await _cloudinary.DeleteFileAsync(videoPublicId);

                if (!string.IsNullOrEmpty(docPublicId))
                    await _cloudinary.DeleteFileAsync(docPublicId);

                return BaseResponse<LessonResponse>.Error($"Error: {ex.Message}");
            }

            int nextPosition = 1;
            if (selectedUnit.Lessons.Any())
            {
                nextPosition = selectedUnit.Lessons.Max(u => u.Position) + 1;
            }
            var newLesson = new Lesson
            {
                LessonID = Guid.NewGuid(),
                Title = request.Title,
                Content = request.Content,
                Position = nextPosition,
                SkillFocus = request.SkillFocus,
                Description = request.Description,
                VideoUrl = videoUrl,
                VideoPublicId = videoPublicId,
                DocumentUrl = docUrl,
                DocumentPublicId = docPublicId,
                CourseUnitID = unitId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };


            try
            {
                var result = await _unit.Lessons.CreateAsync(newLesson);
                if (result < 0)
                {
                    return BaseResponse<LessonResponse>.Fail("Failed when creating a lesson.");
                }

                selectedCourse.NumLessons += 1;
                selectedUnit.TotalLessons += 1;

                await _unit.SaveChangesAsync();

                var response = new LessonResponse
                {
                    LessonID = newLesson.LessonID,
                    Title = newLesson.Title,
                    Content = newLesson.Content,
                    Position = newLesson.Position,
                    SkillFocus = newLesson.SkillFocus.ToString(),
                    Description = newLesson.Description,
                    VideoUrl = newLesson.VideoUrl,
                    DocumentUrl = newLesson.DocumentUrl,
                    CourseID = selectedCourse.CourseID,
                    CourseTitle = selectedCourse.Title,
                    CourseUnitID = selectedUnit.CourseUnitID,
                    UnitTitle = selectedUnit.Title,
                    CreatedAt = newLesson.CreatedAt,
                    UpdatedAt = newLesson.UpdatedAt,
                };

                return BaseResponse<LessonResponse>.Success(response);

            }
            catch (Exception ex)
            {
                return BaseResponse<LessonResponse>.Error($"Error: {ex.Message}");
            }
        }

        public async Task<BaseResponse<LessonResponse>> GetLessonByIdAsync(Guid lessonId)
        {
            var lesson = await _unit.Lessons.Query()
                .Include(l => l.CourseUnit)
                .ThenInclude(u => u.Course)
                .FirstOrDefaultAsync(l => l.LessonID == lessonId);

            if (lesson == null)
            {
                return BaseResponse<LessonResponse>.Fail("Lesson not found.");
            }

            var response = new LessonResponse
            {
                LessonID = lesson.LessonID,
                Title = lesson.Title,
                Content = lesson.Content,
                Position = lesson.Position,
                SkillFocus = lesson.SkillFocus.ToString(),
                Description = lesson.Description,
                VideoUrl = lesson.VideoUrl,
                DocumentUrl = lesson.DocumentUrl,
                CourseID = lesson.CourseUnit?.Course?.CourseID ?? Guid.Empty,
                CourseTitle = lesson.CourseUnit?.Course?.Title,
                CourseUnitID = lesson.CourseUnitID,
                UnitTitle = lesson.CourseUnit?.Title,
                CreatedAt = lesson.CreatedAt,
                UpdatedAt = lesson.UpdatedAt
            };

            return BaseResponse<LessonResponse>.Success(response);
        }

        public async Task<PagedResponse<IEnumerable<LessonResponse>>> GetLessonsByCourseIdAsync(
            Guid courseId,
            PagingRequest request)
        {
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

            var query = _unit.Lessons.Query()
                .Include(l => l.CourseUnit)
                .ThenInclude(u => u.Course)
                .Where(l => l.CourseUnit!.CourseID == courseId)
                .OrderBy(l => l.Position);

            var totalItems = await query.CountAsync();

            var lessons = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var lessonResponses = lessons.Select(l => new LessonResponse
            {
                LessonID = l.LessonID,
                Title = l.Title,
                Content = l.Content,
                Position = l.Position,
                SkillFocus = l.SkillFocus.ToString(),
                Description = l.Description,
                VideoUrl = l.VideoUrl,
                DocumentUrl = l.DocumentUrl,
                CourseID = l.CourseUnit?.Course?.CourseID ?? Guid.Empty,
                CourseTitle = l.CourseUnit?.Course?.Title,
                CourseUnitID = l.CourseUnitID,
                UnitTitle = l.CourseUnit?.Title,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            });

            return PagedResponse<IEnumerable<LessonResponse>>.Success(
                lessonResponses,
                page: page,
                pageSize: pageSize,
                totalItems: totalItems
            );
        }


        public async Task<PagedResponse<IEnumerable<LessonResponse>>> GetLessonsByUnitIdAsync(Guid unitId, PagingRequest request)
        {
            var query = _unit.Lessons.Query()
                .Include(l => l.CourseUnit)
                .ThenInclude(u => u.Course)
                .Where(l => l.CourseUnitID == unitId)
                .OrderBy(l => l.Position);

            var totalItems = await query.CountAsync();

            var lessons = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var lessonResponses = lessons.Select(l => new LessonResponse
            {
                LessonID = l.LessonID,
                Title = l.Title,
                Content = l.Content,
                Position = l.Position,
                SkillFocus = l.SkillFocus.ToString(),
                Description = l.Description,
                VideoUrl = l.VideoUrl,
                DocumentUrl = l.DocumentUrl,
                CourseID = l.CourseUnit?.Course?.CourseID ?? Guid.Empty,
                CourseTitle = l.CourseUnit?.Course?.Title,
                CourseUnitID = l.CourseUnitID,
                UnitTitle = l.CourseUnit?.Title,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            });

            return PagedResponse<IEnumerable<LessonResponse>>.Success(
                lessonResponses,
                request.Page,
                request.PageSize,
                totalItems
            );
        }

        public async Task<BaseResponse<LessonResponse>> UpdateLessonAsync(
            Guid teacherId,
            Guid courseId,
            Guid unitId,
            Guid lessonId,
            LessonRequest request)
        {
            var teacher = await _unit.Users.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == teacherId);

            if (teacher == null || !teacher.UserRoles.Any(ur => ur.Role.Name == "Teacher"))
                return BaseResponse<LessonResponse>.Fail("Invalid TeacherID. Teacher not found or does not have role 'Teacher'.");

            var selectedCourse = await _unit.Courses.Query()
                .Include(c => c.CourseUnits)
                .FirstOrDefaultAsync(c => c.CourseID == courseId);

            if (selectedCourse == null)
                return BaseResponse<LessonResponse>.Fail("Selected course not found.");

            if (selectedCourse.Status != CourseStatus.Draft && selectedCourse.Status != CourseStatus.Rejected)
                return BaseResponse<LessonResponse>.Fail("Only Draft or Rejected courses can be updated.");

            var selectedUnit = await _unit.CourseUnits.Query()
                .Include(u => u.Lessons)
                .Where(u => u.CourseID == courseId && u.CourseUnitID == unitId)
                .FirstOrDefaultAsync();

            if (selectedUnit == null)
                return BaseResponse<LessonResponse>.Fail("Unit not found or not in course.");

            var lesson = await _unit.Lessons.GetByIdAsync(lessonId);
            if (lesson == null)
                return BaseResponse<LessonResponse>.Fail("Lesson not found in the specified unit.");

            string? newVideoUrl = null;
            string? newVideoPublicId = null;
            string? newDocUrl = null;
            string? newDocPublicId = null;

            try
            {
                if (request.VideoFile != null)
                {
                    var video = await _cloudinary.UploadVideoAsync(request.VideoFile, "lessons/videos");
                    newVideoUrl = video.Url;
                    newVideoPublicId = video.PublicId;
                }

                if (request.DocumentFile != null)
                {
                    var doc = await _cloudinary.UploadDocumentAsync(request.DocumentFile, "lessons/documents");
                    newDocUrl = doc.Url;
                    newDocPublicId = doc.PublicId;
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(newVideoPublicId))
                    await _cloudinary.DeleteFileAsync(newVideoPublicId);

                if (!string.IsNullOrEmpty(newDocPublicId))
                    await _cloudinary.DeleteFileAsync(newDocPublicId);

                return BaseResponse<LessonResponse>.Error($"Upload file failed: {ex.Message}");
            }

            lesson.Title = request.Title;
            lesson.Content = request.Content;
            lesson.SkillFocus = request.SkillFocus;
            lesson.Description = request.Description;
            lesson.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(newVideoUrl))
            {
                if (!string.IsNullOrEmpty(lesson.VideoPublicId))
                    await _cloudinary.DeleteFileAsync(lesson.VideoPublicId);

                lesson.VideoUrl = newVideoUrl;
                lesson.VideoPublicId = newVideoPublicId;
            }

            if (!string.IsNullOrEmpty(newDocUrl))
            {
                if (!string.IsNullOrEmpty(lesson.DocumentPublicId))
                    await _cloudinary.DeleteFileAsync(lesson.DocumentPublicId);

                lesson.DocumentUrl = newDocUrl;
                lesson.DocumentPublicId = newDocPublicId;
            }

            try
            {
                var result = await _unit.Lessons.UpdateAsync(lesson);
                if (result < 0)
                    return BaseResponse<LessonResponse>.Fail("Failed to update the lesson.");

                await _unit.SaveChangesAsync();

                var response = new LessonResponse
                {
                    LessonID = lesson.LessonID,
                    Title = lesson.Title,
                    Content = lesson.Content,
                    Position = lesson.Position,
                    SkillFocus = lesson.SkillFocus.ToString(),
                    Description = lesson.Description,
                    VideoUrl = lesson.VideoUrl,
                    DocumentUrl = lesson.DocumentUrl,
                    CourseID = selectedCourse.CourseID,
                    CourseTitle = selectedCourse.Title,
                    CourseUnitID = selectedUnit.CourseUnitID,
                    UnitTitle = selectedUnit.Title,
                    CreatedAt = lesson.CreatedAt,
                    UpdatedAt = lesson.UpdatedAt,
                };

                return BaseResponse<LessonResponse>.Success(response);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(newVideoPublicId))
                    await _cloudinary.DeleteFileAsync(newVideoPublicId);
                if (!string.IsNullOrEmpty(newDocPublicId))
                    await _cloudinary.DeleteFileAsync(newDocPublicId);

                return BaseResponse<LessonResponse>.Error($"Error: {ex.Message}");
            }
        }

    }
}
