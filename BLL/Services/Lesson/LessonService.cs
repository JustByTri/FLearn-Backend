using BLL.IServices.Lesson;
using BLL.IServices.Upload;
using Common.DTO.ApiResponse;
using Common.DTO.Lesson.Request;
using Common.DTO.Lesson.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.Helpers;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Lesson
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
        public async Task<BaseResponse<LessonResponse>> CreateLessonAsync(Guid userId, Guid unitId, LessonRequest request)
        {
            try
            {
                var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
                if (teacher == null)
                    return BaseResponse<LessonResponse>.Fail("Teacher does not exist.");

                var selectedUnit = await _unit.CourseUnits.Query()
                    .Include(u => u.Course)
                    .Include(u => u.Lessons)
                    .FirstOrDefaultAsync(u => u.CourseUnitID == unitId);

                if (selectedUnit == null)
                    return BaseResponse<LessonResponse>.Fail(
                        new { UnitId = "Unit not found." },
                        "Unit does not exist.",
                        404
                    );

                var selectedCourse = selectedUnit.Course;
                if (selectedCourse == null)
                    return BaseResponse<LessonResponse>.Fail(
                        new { Course = "Course not found for this unit." },
                        "Invalid course reference.",
                        404
                    );

                if (selectedCourse.Status != CourseStatus.Draft && selectedCourse.Status != CourseStatus.Rejected)
                {
                    return BaseResponse<LessonResponse>.Fail(
                        new { CourseStatus = "Invalid course status." },
                        "Only Draft or Rejected courses can be updated.",
                        400
                    );
                }

                string? videoUrl = null;
                string? videoPublicId = null;
                string? docUrl = null;
                string? docPublicId = null;

                try
                {
                    if (request.VideoFile != null)
                    {
                        if (!request.VideoFile.ContentType.StartsWith("video/"))
                            return BaseResponse<LessonResponse>.Fail(
                                new { VideoFile = "Invalid video file format." },
                                "Only video files are allowed.",
                                400
                            );

                        var video = await _cloudinary.UploadVideoAsync(request.VideoFile, "lessons/videos");
                        videoUrl = video.Url;
                        videoPublicId = video.PublicId;
                    }

                    if (request.DocumentFile != null)
                    {
                        if (!request.DocumentFile.ContentType.Contains("pdf") &&
                            !request.DocumentFile.ContentType.Contains("msword") &&
                            !request.DocumentFile.ContentType.Contains("officedocument"))
                        {
                            return BaseResponse<LessonResponse>.Fail(
                                new { DocumentFile = "Invalid document file format." },
                                "Only PDF or Word documents are allowed.",
                                400
                            );
                        }

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

                    return BaseResponse<LessonResponse>.Error($"Upload file failed: {ex.Message}", 500);
                }

                int nextPosition = selectedUnit.Lessons.Any()
                    ? selectedUnit.Lessons.Max(u => u.Position) + 1
                    : 1;

                var newLesson = new DAL.Models.Lesson
                {
                    LessonID = Guid.NewGuid(),
                    Title = request.Title.Trim(),
                    Description = request.Description.Trim(),
                    Content = request.Content,
                    Position = nextPosition,
                    VideoUrl = videoUrl,
                    VideoPublicId = videoPublicId,
                    DocumentUrl = docUrl,
                    DocumentPublicId = docPublicId,
                    CourseUnitID = selectedUnit.CourseUnitID,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
                };

                var result = await _unit.Lessons.CreateAsync(newLesson);
                if (result <= 0)
                {
                    if (!string.IsNullOrEmpty(videoPublicId))
                        await _cloudinary.DeleteFileAsync(videoPublicId);
                    if (!string.IsNullOrEmpty(docPublicId))
                        await _cloudinary.DeleteFileAsync(docPublicId);

                    return BaseResponse<LessonResponse>.Fail(
                        "Failed when creating a lesson."
                    );
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
                    Description = newLesson.Description,
                    TotalExercises = newLesson.TotalExercises ?? 0,
                    VideoUrl = newLesson.VideoUrl,
                    DocumentUrl = newLesson.DocumentUrl,
                    CourseID = selectedCourse.CourseID,
                    CourseTitle = selectedCourse.Title,
                    CourseUnitID = selectedUnit.CourseUnitID,
                    UnitTitle = selectedUnit.Title,
                    CreatedAt = newLesson.CreatedAt,
                    UpdatedAt = newLesson.UpdatedAt
                };

                return BaseResponse<LessonResponse>.Success(response, "Lesson created successfully", 201);
            }
            catch (Exception ex)
            {
                return BaseResponse<LessonResponse>.Error("An error occurred while creating lesson.", 500, ex.Message);
            }
        }
        public async Task<BaseResponse<LessonResponse>> GetLessonByIdAsync(Guid lessonId)
        {
            try
            {
                var lesson = await _unit.Lessons.Query()
                    .Include(l => l.CourseUnit)
                    .ThenInclude(u => u.Course)
                    .FirstOrDefaultAsync(l => l.LessonID == lessonId);

                if (lesson == null)
                {
                    return BaseResponse<LessonResponse>.Fail(
                        new { LessonId = "Lesson not found." },
                        "Lesson does not exist.",
                        404
                    );
                }

                var response = new LessonResponse
                {
                    LessonID = lesson.LessonID,
                    Title = lesson.Title,
                    Description = lesson.Description,
                    Content = lesson.Content,
                    Position = lesson.Position,
                    VideoUrl = lesson.VideoUrl,
                    DocumentUrl = lesson.DocumentUrl,
                    CourseID = lesson.CourseUnit.CourseID,
                    TotalExercises = lesson.TotalExercises ?? 0,
                    CourseTitle = lesson.CourseUnit.Course?.Title,
                    CourseUnitID = lesson.CourseUnitID,
                    UnitTitle = lesson.CourseUnit?.Title,
                    CreatedAt = lesson.CreatedAt,
                    UpdatedAt = lesson.UpdatedAt
                };

                return BaseResponse<LessonResponse>.Success(response, "Lesson retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return BaseResponse<LessonResponse>.Error("An error occurred while retrieving lesson.", 500, ex.Message);
            }
        }
        public async Task<PagedResponse<IEnumerable<LessonResponse>>> GetLessonsByUnitIdAsync(Guid unitId, PagingRequest request)
        {
            try
            {
                var unit = await _unit.CourseUnits.Query()
                    .Include(u => u.Lessons)
                    .Include(u => u.Course)
                    .FirstOrDefaultAsync(u => u.CourseUnitID == unitId);

                if (unit == null)
                {
                    return new PagedResponse<IEnumerable<LessonResponse>>
                    {
                        Status = "fail",
                        Code = 404,
                        Message = "Unit not found.",
                        Errors = new { UnitId = "Unit does not exist." }
                    };
                }

                var lessonsQuery = unit.Lessons.AsQueryable().OrderBy(l => l.Position);

                int totalItems = lessonsQuery.Count();

                var lessons = lessonsQuery
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(l => new LessonResponse
                    {
                        LessonID = l.LessonID,
                        Title = l.Title,
                        Description = l.Description,
                        TotalExercises = l.TotalExercises ?? 0,
                        Content = l.Content,
                        Position = l.Position,
                        VideoUrl = l.VideoUrl,
                        DocumentUrl = l.DocumentUrl,
                        CourseID = unit.CourseID,
                        CourseTitle = unit.Course != null ? unit.Course.Title : null,
                        CourseUnitID = unit.CourseUnitID,
                        UnitTitle = unit.Title,
                        CreatedAt = l.CreatedAt,
                        UpdatedAt = l.UpdatedAt
                    })
                    .ToList();

                return PagedResponse<IEnumerable<LessonResponse>>.Success(
                    data: lessons,
                    page: request.Page,
                    pageSize: request.PageSize,
                    totalItems: totalItems,
                    message: "Lessons retrieved successfully",
                    code: 200
                );
            }
            catch (Exception ex)
            {
                return new PagedResponse<IEnumerable<LessonResponse>>
                {
                    Status = "error",
                    Code = 500,
                    Message = "An error occurred while retrieving lessons.",
                    Errors = ex.Message
                };
            }
        }
        public async Task<BaseResponse<LessonResponse>> UpdateLessonAsync(Guid userId, Guid unitId, Guid lessonId, LessonUpdateRequest request)
        {
            try
            {
                var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
                if (teacher == null)
                    return BaseResponse<LessonResponse>.Fail("Teacher does not exist.");


                var selectedUnit = await _unit.CourseUnits.Query()
                    .Include(u => u.Course)
                    .Include(u => u.Lessons)
                    .FirstOrDefaultAsync(u => u.CourseUnitID == unitId);

                if (selectedUnit == null)
                    return BaseResponse<LessonResponse>.Fail(
                        new { UnitId = "Unit not found." },
                        "Unit does not exist.",
                        404
                    );

                var selectedCourse = selectedUnit.Course;
                if (selectedCourse == null)
                    return BaseResponse<LessonResponse>.Fail(
                        new { Course = "Course not found for this unit." },
                        "Invalid course reference.",
                        404
                    );

                if (selectedCourse.TeacherId != teacher.TeacherProfileId)
                {
                    return BaseResponse<LessonResponse>.Fail(null,
                        "You do not have permission to modify this lesson because you are not the owner of this course.",
                        403
                    );
                }

                if (selectedCourse.Status != CourseStatus.Draft && selectedCourse.Status != CourseStatus.Rejected)
                {
                    return BaseResponse<LessonResponse>.Fail(
                        new { CourseStatus = "Invalid course status." },
                        "Only Draft or Rejected courses can be updated.",
                        400
                    );
                }

                var existingLesson = selectedUnit.Lessons.FirstOrDefault(l => l.LessonID == lessonId);
                if (existingLesson == null)
                    return BaseResponse<LessonResponse>.Fail(
                        new { Lesson = "Lesson not found." },
                        "Lesson does not exist.",
                        404
                    );

                string? newVideoUrl = null;
                string? newVideoPublicId = null;
                string? newDocUrl = null;
                string? newDocPublicId = null;

                try
                {
                    if (request.VideoFile != null)
                    {
                        if (!request.VideoFile.ContentType.StartsWith("video/"))
                            return BaseResponse<LessonResponse>.Fail(
                                new { VideoFile = "Invalid video file format." },
                                "Only video files are allowed.",
                                400
                            );

                        if (!string.IsNullOrEmpty(existingLesson.VideoPublicId))
                            await _cloudinary.DeleteFileAsync(existingLesson.VideoPublicId);

                        var video = await _cloudinary.UploadVideoAsync(request.VideoFile, "lessons/videos");
                        newVideoUrl = video.Url;
                        newVideoPublicId = video.PublicId;
                    }

                    if (request.DocumentFile != null)
                    {
                        if (!request.DocumentFile.ContentType.Contains("pdf") &&
                            !request.DocumentFile.ContentType.Contains("msword") &&
                            !request.DocumentFile.ContentType.Contains("officedocument"))
                        {
                            return BaseResponse<LessonResponse>.Fail(
                                new { DocumentFile = "Invalid document file format." },
                                "Only PDF or Word documents are allowed.",
                                400
                            );
                        }

                        if (!string.IsNullOrEmpty(existingLesson.DocumentPublicId))
                            await _cloudinary.DeleteFileAsync(existingLesson.DocumentPublicId);

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

                    return BaseResponse<LessonResponse>.Error($"Upload file failed: {ex.Message}", 500);
                }

                existingLesson.Title = !string.IsNullOrWhiteSpace(request.Title) ? request.Title.Trim() : existingLesson.Title;
                existingLesson.Description = !string.IsNullOrWhiteSpace(request.Description) ? request.Description.Trim() : existingLesson.Description;
                existingLesson.Content = !string.IsNullOrWhiteSpace(request.Content) ? request.Content : existingLesson.Content;

                if (newVideoUrl != null)
                {
                    existingLesson.VideoUrl = newVideoUrl;
                    existingLesson.VideoPublicId = newVideoPublicId;
                }

                if (newDocUrl != null)
                {
                    existingLesson.DocumentUrl = newDocUrl;
                    existingLesson.DocumentPublicId = newDocPublicId;
                }

                existingLesson.UpdatedAt = TimeHelper.GetVietnamTime();

                var result = await _unit.Lessons.UpdateAsync(existingLesson);
                if (result <= 0)
                {
                    return BaseResponse<LessonResponse>.Fail("Failed to update lesson.");
                }

                await _unit.SaveChangesAsync();

                var response = new LessonResponse
                {
                    LessonID = existingLesson.LessonID,
                    Title = existingLesson.Title,
                    Description = existingLesson.Description,
                    TotalExercises = existingLesson.TotalExercises ?? 0,
                    Content = existingLesson.Content,
                    Position = existingLesson.Position,
                    VideoUrl = existingLesson.VideoUrl,
                    DocumentUrl = existingLesson.DocumentUrl,
                    CourseID = selectedCourse.CourseID,
                    CourseTitle = selectedCourse.Title,
                    CourseUnitID = selectedUnit.CourseUnitID,
                    UnitTitle = selectedUnit.Title,
                    CreatedAt = existingLesson.CreatedAt,
                    UpdatedAt = existingLesson.UpdatedAt
                };

                return BaseResponse<LessonResponse>.Success(response, "Lesson updated successfully", 200);
            }
            catch (Exception ex)
            {
                return BaseResponse<LessonResponse>.Error("An error occurred while updating lesson.", 500, ex.Message);
            }
        }
    }
}

