using BLL.IServices.Exercise;
using BLL.IServices.Upload;
using Common.DTO.ApiResponse;
using Common.DTO.Exercise.Request;
using Common.DTO.Exercise.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.Upload;
using DAL.Helpers;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Exercise
{
    public class ExerciseService : IExerciseService
    {
        private readonly IUnitOfWork _unit;
        private readonly ICloudinaryService _cloudinary;
        public ExerciseService(IUnitOfWork unit, ICloudinaryService cloudinary)
        {
            _unit = unit;
            _cloudinary = cloudinary;
        }
        private bool IsImage(IFormFile file) => file.ContentType.ToLower().StartsWith("image/");
        private bool IsAudio(IFormFile file) => file.ContentType.ToLower().StartsWith("audio/") || file.ContentType == "video/webm";
        public async Task<BaseResponse<ExerciseResponse>> CreateExerciseAsync(Guid userId, Guid lessonId, ExerciseRequest request)
        {
            try
            {
                var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
                if (teacher == null || !teacher.Status)
                    return BaseResponse<ExerciseResponse>.Fail(new object(), "Access denied: the teacher profile is invalid or inactive.", 403);

                var selectedLesson = await _unit.Lessons.Query()
                    .Include(l => l.CourseUnit)
                        .ThenInclude(u => u.Course)
                            .ThenInclude(c => c.Template)
                    .FirstOrDefaultAsync(l => l.LessonID == lessonId);

                if (selectedLesson == null)
                    return BaseResponse<ExerciseResponse>.Fail(new object(), "Lesson not found.", 404);

                var selectedUnit = selectedLesson.CourseUnit;
                if (selectedUnit == null)
                    return BaseResponse<ExerciseResponse>.Fail(new object(), "Course unit not found.", 404);

                var selectedCourse = selectedUnit.Course;
                if (selectedCourse == null)
                    return BaseResponse<ExerciseResponse>.Fail(new object(), "Course not found.", 404);

                if (selectedCourse.Status != CourseStatus.Draft && selectedCourse.Status != CourseStatus.Rejected)
                    return BaseResponse<ExerciseResponse>.Fail(new object(), "Only Draft or Rejected courses can be modified.", 400);

                var files = request.MediaFiles ?? new List<IFormFile>();
                if (request.Type == SpeakingExerciseType.RepeatAfterMe)
                {
                    if (files.Count != 1)
                        return BaseResponse<ExerciseResponse>.Fail(new { MediaFiles = "Count" }, "Repeat After Me exercise requires exactly one audio file.", 400);

                    if (!IsAudio(files[0]))
                        return BaseResponse<ExerciseResponse>.Fail(new { MediaFiles = "Format" }, "Repeat After Me exercise only allows audio file.", 400);
                }
                else if (request.Type == SpeakingExerciseType.PictureDescription || request.Type == SpeakingExerciseType.StoryTelling)
                {
                    if (files.Count == 0)
                        return BaseResponse<ExerciseResponse>.Fail(new { MediaFiles = "Count" }, "This exercise type requires at least one image.", 400);

                    if (files.Any(f => !IsImage(f)))
                        return BaseResponse<ExerciseResponse>.Fail(new { MediaFiles = "Format" }, "This exercise type only allows image files.", 400);
                }
                else if (request.Type == SpeakingExerciseType.Debate)
                {
                    if (files.Any() && files.Any(f => !IsImage(f)))
                        return BaseResponse<ExerciseResponse>.Fail(new { MediaFiles = "Format" }, "Debate exercise only allows image files.", 400);
                }
                else
                {
                    if (files.Any(f => !IsImage(f) && !IsAudio(f)))
                        return BaseResponse<ExerciseResponse>.Fail(new { MediaFiles = "Format" }, "Invalid file format.", 400);
                }

                var mediaUrls = new List<string>();
                var mediaPublicIds = new List<string>();

                foreach (var mediaFile in files)
                {
                    if (mediaFile.Length == 0) continue;

                    UploadResultDto uploadResult;
                    if (IsImage(mediaFile))
                        uploadResult = await _cloudinary.UploadImageAsync(mediaFile, "exercises/images");
                    else
                        uploadResult = await _cloudinary.UploadAudioAsync(mediaFile, "exercises/audio");

                    if (uploadResult != null && !string.IsNullOrEmpty(uploadResult.Url))
                    {
                        mediaUrls.Add(uploadResult.Url);
                        mediaPublicIds.Add(uploadResult.PublicId);
                    }
                }

                int maxExercises = selectedCourse.Template?.ExercisesPerLesson ?? 12;

                var existingExercises = await _unit.Exercises.FindAllAsync(ex => ex.LessonID == selectedLesson.LessonID);
                if ((existingExercises?.Count() ?? 0) >= maxExercises)
                    return BaseResponse<ExerciseResponse>.Fail(new object(), $"Cannot add more exercises. Maximum allowed exercises for this lesson is {maxExercises}.");

                if (request.PassScore > request.MaxScore)
                    return BaseResponse<ExerciseResponse>.Fail(new object(), "Pass score cannot exceed max score.", 400);

                int nextPosition = (existingExercises != null && existingExercises.Any()) ? existingExercises.Max(e => e.Position) + 1 : 1;

                var now = TimeHelper.GetVietnamTime();
                var newExercise = new DAL.Models.Exercise
                {
                    ExerciseID = Guid.NewGuid(),
                    Title = request.Title.Trim(),
                    Prompt = string.IsNullOrWhiteSpace(request.Prompt) ? string.Empty : request.Prompt.Trim(),
                    Hints = string.IsNullOrWhiteSpace(request.Hints) ? string.Empty : request.Hints.Trim(),
                    Content = string.IsNullOrWhiteSpace(request.Content) ? string.Empty : request.Content.Trim(),
                    ExpectedAnswer = string.IsNullOrWhiteSpace(request.ExpectedAnswer) ? string.Empty : request.ExpectedAnswer.Trim(),
                    Type = request.Type,
                    Difficulty = request.Difficulty,
                    Position = nextPosition,
                    MaxScore = request.MaxScore,
                    PassScore = request.PassScore,
                    FeedbackCorrect = string.IsNullOrWhiteSpace(request.FeedbackCorrect) ? string.Empty : request.FeedbackCorrect.Trim(),
                    FeedbackIncorrect = string.IsNullOrWhiteSpace(request.FeedbackIncorrect) ? string.Empty : request.FeedbackIncorrect.Trim(),
                    LessonID = selectedLesson.LessonID,
                    MediaUrl = mediaUrls.Any() ? string.Join(";", mediaUrls) : string.Empty,
                    MediaPublicId = mediaPublicIds.Any() ? string.Join(";", mediaPublicIds) : string.Empty,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _unit.Exercises.CreateAsync(newExercise);
                selectedLesson.TotalExercises += 1;
                await _unit.Lessons.UpdateAsync(selectedLesson);

                if (selectedCourse.Status == CourseStatus.Rejected)
                {
                    selectedCourse.Status = CourseStatus.Draft;
                }

                await _unit.SaveChangesAsync();

                var response = new ExerciseResponse
                {
                    ExerciseID = newExercise.ExerciseID,
                    Title = newExercise.Title,
                    Prompt = newExercise.Prompt,
                    Hints = newExercise.Hints,
                    Content = newExercise.Content,
                    ExpectedAnswer = newExercise.ExpectedAnswer,
                    MediaUrls = mediaUrls.Any() ? mediaUrls.ToArray() : null,
                    MediaPublicIds = mediaPublicIds.Any() ? mediaPublicIds.ToArray() : null,
                    Position = newExercise.Position,
                    ExerciseType = newExercise.Type.ToString(),
                    Difficulty = newExercise.Difficulty.ToString(),
                    MaxScore = newExercise.MaxScore,
                    PassScore = newExercise.PassScore,
                    FeedbackCorrect = newExercise.FeedbackCorrect,
                    FeedbackIncorrect = newExercise.FeedbackIncorrect,
                    CourseID = selectedCourse.CourseID,
                    CourseTitle = selectedCourse.Title,
                    UnitID = selectedUnit.CourseUnitID,
                    UnitTitle = selectedUnit.Title,
                    LessonID = selectedLesson.LessonID,
                    LessonTitle = selectedLesson.Title,
                    CreatedAt = newExercise.CreatedAt.ToString("dd-MM-yyyy"),
                    UpdatedAt = newExercise.UpdatedAt.ToString("dd-MM-yyyy")
                };

                return BaseResponse<ExerciseResponse>.Success(response, "Exercise created successfully.", 201);
            }
            catch (Exception ex)
            {
                return BaseResponse<ExerciseResponse>.Error("An error occurred while creating exercise.", 500, ex.Message);
            }
        }
        public async Task<BaseResponse<ExerciseResponse>> DeleteExerciseByIdAsync(Guid userId, Guid exerciseId)
        {
            try
            {
                var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
                if (teacher == null)
                    return BaseResponse<ExerciseResponse>.Fail(
                        new { Access = "Unauthorized" },
                        "Access denied. Teacher not found.",
                        403
                    );

                var exercise = await _unit.Exercises.Query()
                    .Include(e => e.Lesson)
                        .ThenInclude(l => l.CourseUnit)
                            .ThenInclude(u => u.Course)
                    .Include(e => e.ExerciseSubmissions)
                    .FirstOrDefaultAsync(e => e.ExerciseID == exerciseId);

                if (exercise == null)
                    return BaseResponse<ExerciseResponse>.Fail(
                        new { ExerciseId = "Not found" },
                        "Exercise not found.",
                        404
                    );

                var course = exercise.Lesson?.CourseUnit?.Course;
                if (course == null)
                    return BaseResponse<ExerciseResponse>.Fail(
                        new { Course = "Invalid reference" },
                        "Course reference is invalid.",
                        500
                    );

                if (course.TeacherId != teacher.TeacherId)
                    return BaseResponse<ExerciseResponse>.Fail(
                        new { Permission = "Forbidden" },
                        "You do not have permission to delete this exercise.",
                        403
                    );

                if (course.Status != CourseStatus.Draft && course.Status != CourseStatus.Rejected)
                    return BaseResponse<ExerciseResponse>.Fail(
                        new { CourseStatus = course.Status.ToString() },
                        "Only exercises in Draft or Rejected courses can be deleted.",
                        400
                    );

                return await _unit.ExecuteInTransactionAsync(async () =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(exercise.MediaPublicId) && exercise.MediaPublicId != "null!")
                        {
                            var publicIds = exercise.MediaPublicId.Split(';', StringSplitOptions.RemoveEmptyEntries);
                            foreach (var publicId in publicIds)
                            {
                                if (!string.IsNullOrEmpty(publicId) && publicId != "null!")
                                {
                                    await _cloudinary.DeleteFileAsync(publicId);
                                }
                            }
                        }

                        if (exercise.ExerciseSubmissions?.Any() == true)
                        {
                            foreach (var submission in exercise.ExerciseSubmissions.ToList())
                            {
                                await _unit.ExerciseSubmissions.RemoveAsync(submission);
                            }
                        }

                        var lesson = exercise.Lesson;
                        if (lesson != null)
                        {
                            lesson.TotalExercises = Math.Max(0, lesson.TotalExercises - 1);
                            await _unit.Lessons.UpdateAsync(lesson);
                        }

                        var remainingExercises = await _unit.Exercises.FindAllAsync(
                            e => e.LessonID == exercise.LessonID && e.ExerciseID != exerciseId);

                        foreach (var remainingExercise in remainingExercises.OrderBy(e => e.Position))
                        {
                            if (remainingExercise.Position > exercise.Position)
                            {
                                remainingExercise.Position--;
                                await _unit.Exercises.UpdateAsync(remainingExercise);
                            }
                        }

                        var deleted = await _unit.Exercises.RemoveAsync(exercise);
                        if (!deleted)
                        {
                            return BaseResponse<ExerciseResponse>.Fail(
                                new { Delete = "Failed" },
                                "Failed to delete exercise from database.",
                                500
                            );
                        }

                        await _unit.SaveChangesAsync();

                        return BaseResponse<ExerciseResponse>.Success(
                            null,
                            $"Exercise '{exercise.Title}' deleted successfully.",
                            200
                        );
                    }
                    catch (Exception ex)
                    {
                        return BaseResponse<ExerciseResponse>.Error(
                            "Transaction failed while deleting exercise.",
                            500,
                            ex.Message
                        );
                    }
                });
            }
            catch (Exception ex)
            {
                return BaseResponse<ExerciseResponse>.Error(
                    "An unexpected error occurred while deleting exercise.",
                    500,
                    ex.Message
                );
            }
        }
        public async Task<BaseResponse<ExerciseResponse>> GetExerciseByIdAsync(Guid exerciseId)
        {
            try
            {
                var exercise = await _unit.Exercises.Query()
                    .Include(e => e.Lesson)
                        .ThenInclude(l => l.CourseUnit)
                        .ThenInclude(u => u.Course)
                    .FirstOrDefaultAsync(e => e.ExerciseID == exerciseId);

                if (exercise == null)
                    return BaseResponse<ExerciseResponse>.Fail(null, "Exercise not found.", 404);

                var response = new ExerciseResponse
                {
                    ExerciseID = exercise.ExerciseID,
                    LessonID = exercise.LessonID,
                    Title = exercise.Title,
                    Prompt = exercise.Prompt,
                    Hints = exercise.Hints,
                    Content = exercise.Content,
                    MediaUrls = exercise.MediaUrl != null ? exercise.MediaUrl.Split(';') : null,
                    MediaPublicIds = exercise.MediaPublicId != null ? exercise.MediaPublicId.Split(';') : null,
                    ExpectedAnswer = exercise.ExpectedAnswer,
                    ExerciseType = exercise.Type.ToString(),
                    Difficulty = exercise.Difficulty.ToString(),
                    MaxScore = exercise.MaxScore,
                    PassScore = exercise.PassScore,
                    LessonTitle = exercise.Lesson.Title,
                    Position = exercise.Position,
                    CourseID = exercise.Lesson.CourseUnit.CourseID,
                    CourseTitle = exercise.Lesson.CourseUnit.Course.Title,
                    UnitID = exercise.Lesson.CourseUnit.CourseUnitID,
                    UnitTitle = exercise.Lesson.CourseUnit.Title,
                    FeedbackCorrect = exercise.FeedbackCorrect,
                    FeedbackIncorrect = exercise.FeedbackIncorrect,
                    CreatedAt = exercise.CreatedAt.ToString("dd-MM-yyyy"),
                    UpdatedAt = exercise.UpdatedAt.ToString("dd-MM-yyyy")
                };

                return BaseResponse<ExerciseResponse>.Success(response, "Exercise retrieved successfully.", 200);
            }
            catch (Exception ex)
            {
                return BaseResponse<ExerciseResponse>.Error("An error occurred while retrieving exercise.", 500, ex.Message);
            }
        }
        public async Task<PagedResponse<IEnumerable<ExerciseResponse>>> GetExercisesByLessonIdAsync(Guid lessonId, PagingRequest request)
        {
            try
            {
                var lesson = await _unit.Lessons.Query()
                    .Include(l => l.Exercises)
                    .Include(l => l.CourseUnit)
                    .ThenInclude(u => u.Course)
                    .FirstOrDefaultAsync(l => l.LessonID == lessonId);

                if (lesson == null)
                    return (PagedResponse<IEnumerable<ExerciseResponse>>)PagedResponse<IEnumerable<ExerciseResponse>>.Fail(null, "Lesson not found.", 404);

                var exercisesQuery = lesson.Exercises.AsQueryable().OrderBy(e => e.CreatedAt);

                var totalItems = exercisesQuery.Count();
                var exercises = exercisesQuery
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList()
                    .Select(e => new ExerciseResponse
                    {
                        ExerciseID = e.ExerciseID,
                        LessonID = lesson.LessonID,
                        Title = e.Title,
                        Prompt = e.Prompt,
                        Hints = e.Hints,
                        Content = e.Content,
                        MediaUrls = e.MediaUrl != null ? e.MediaUrl.Split(new[] { ';' }, StringSplitOptions.None) : null,
                        MediaPublicIds = e.MediaPublicId != null ? e.MediaPublicId.Split(new[] { ';' }, StringSplitOptions.None) : null,
                        ExpectedAnswer = e.ExpectedAnswer,
                        ExerciseType = e.Type.ToString(),
                        Difficulty = e.Difficulty.ToString(),
                        MaxScore = e.MaxScore,
                        PassScore = e.PassScore,
                        FeedbackCorrect = e.FeedbackCorrect,
                        LessonTitle = e.Lesson.Title,
                        CourseID = e.Lesson.CourseUnit.CourseID,
                        CourseTitle = e.Lesson.CourseUnit.Course.Title,
                        Position = e.Position,
                        UnitID = e.Lesson.CourseUnitID,
                        UnitTitle = e.Lesson.CourseUnit.Title,
                        FeedbackIncorrect = e.FeedbackIncorrect,
                        CreatedAt = e.CreatedAt.ToString("dd-MM-yyyy"),
                        UpdatedAt = e.UpdatedAt.ToString("dd-MM-yyyy")
                    })
                    .ToList();

                return PagedResponse<IEnumerable<ExerciseResponse>>.Success(
                    exercises,
                    request.Page,
                    request.PageSize,
                    totalItems,
                    "Exercises fetched successfully."
                );
            }
            catch (Exception ex)
            {
                return (PagedResponse<IEnumerable<ExerciseResponse>>)PagedResponse<IEnumerable<ExerciseResponse>>.Error("An error occurred while fetching exercises.", 500, ex.Message);
            }
        }
        public async Task<BaseResponse<ExerciseResponse>> UpdateExerciseAsync(Guid userId, Guid lessonId, Guid exerciseId, ExerciseUpdateRequest request)
        {
            try
            {
                // 1. Validate permissions and existence (Giữ nguyên như cũ)
                var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
                if (teacher == null) return BaseResponse<ExerciseResponse>.Fail("Teacher does not exist.");

                var selectedLesson = await _unit.Lessons.Query()
                    .Include(l => l.CourseUnit).ThenInclude(u => u.Course)
                    .Include(l => l.Exercises)
                    .FirstOrDefaultAsync(l => l.LessonID == lessonId);

                if (selectedLesson == null) return BaseResponse<ExerciseResponse>.Fail(new object(), "Lesson not found.", 404);

                var selectedUnit = selectedLesson.CourseUnit;
                if (selectedUnit == null)
                    return BaseResponse<ExerciseResponse>.Fail(new object(), "Unit not found.", 404);

                var selectedCourse = selectedLesson.CourseUnit?.Course;
                if (selectedCourse == null) return BaseResponse<ExerciseResponse>.Fail(new object(), "Course not found.", 404);

                if (selectedCourse.TeacherId != teacher.TeacherId)
                    return BaseResponse<ExerciseResponse>.Fail(null, "Permission denied.", 403);

                if (selectedCourse.Status != CourseStatus.Draft && selectedCourse.Status != CourseStatus.Rejected)
                    return BaseResponse<ExerciseResponse>.Fail(new object(), "Only Draft or Rejected courses can be modified.", 400);

                var selectedExercise = selectedLesson.Exercises.FirstOrDefault(e => e.ExerciseID == exerciseId);
                if (selectedExercise == null) return BaseResponse<ExerciseResponse>.Fail(new object(), "Exercise not found.", 404);

                // --- 2. XÁC ĐỊNH TRẠNG THÁI ---
                var effectiveType = request.Type ?? selectedExercise.Type; // Type sẽ áp dụng
                var files = request.MediaFiles;
                bool hasNewMedia = files != null && files.Any();

                // Helper check URL cũ xem là Audio hay Image (Dựa trên folder Cloudinary đã quy ước lúc Create)
                bool IsUrlImage(string url) => url.Contains("/exercises/images/") || url.Contains("/image/upload/");
                bool IsUrlAudio(string url) => url.Contains("/exercises/audio/") || url.Contains("/video/upload/"); // Audio trên cloudinary thường gộp vào video resource

                // --- 3. LOGIC CHECK TƯƠNG THÍCH KHI ĐỔI TYPE MÀ KHÔNG UPLOAD FILE MỚI ---
                // Nếu user đổi Type VÀ KHÔNG upload file mới -> Phải check file cũ có hợp lệ với Type mới không
                if (request.Type.HasValue && request.Type != selectedExercise.Type && !hasNewMedia)
                {
                    var currentUrls = !string.IsNullOrEmpty(selectedExercise.MediaUrl)
                        ? selectedExercise.MediaUrl.Split(';', StringSplitOptions.RemoveEmptyEntries)
                        : Array.Empty<string>();

                    switch (effectiveType)
                    {
                        case SpeakingExerciseType.RepeatAfterMe:
                            // Yêu cầu: Phải có đúng 1 file và phải là Audio
                            if (currentUrls.Length != 1 || !IsUrlAudio(currentUrls[0]))
                            {
                                return BaseResponse<ExerciseResponse>.Fail(
                                    new { MediaFiles = "Required" },
                                    "Changing to 'Repeat After Me' requires exactly 1 Audio file. The existing media is incompatible. Please upload a new audio file.",
                                    400);
                            }
                            break;

                        case SpeakingExerciseType.PictureDescription:
                        case SpeakingExerciseType.StoryTelling:
                            // Yêu cầu: Phải có ít nhất 1 file và tất cả phải là Image
                            if (currentUrls.Length == 0)
                            {
                                return BaseResponse<ExerciseResponse>.Fail(
                                    new { MediaFiles = "Required" },
                                    "This exercise type requires at least one image. Please upload an image.",
                                    400);
                            }
                            if (currentUrls.Any(u => !IsUrlImage(u)))
                            {
                                return BaseResponse<ExerciseResponse>.Fail(
                                    new { MediaFiles = "Invalid" },
                                    "This exercise type only supports Images. Existing media contains Audio. Please upload new images.",
                                    400);
                            }
                            break;

                        case SpeakingExerciseType.Debate:
                            // Yêu cầu: Nếu có file thì phải là Image (Audio là sai)
                            if (currentUrls.Any(u => IsUrlAudio(u)))
                            {
                                return BaseResponse<ExerciseResponse>.Fail(
                                    new { MediaFiles = "Invalid" },
                                    "Debate exercises cannot contain Audio. Please upload images to replace or clear the media.",
                                    400);
                            }
                            break;
                    }
                }

                // --- 4. VALIDATE FILE MỚI (NẾU CÓ UPLOAD) ---
                if (hasNewMedia)
                {
                    if (effectiveType == SpeakingExerciseType.RepeatAfterMe)
                    {
                        if (files!.Count != 1) return BaseResponse<ExerciseResponse>.Fail(new { MediaFiles = "Count" }, "Repeat After Me requires exactly one audio file.", 400);
                        if (!IsAudio(files[0])) return BaseResponse<ExerciseResponse>.Fail(new { MediaFiles = "Format" }, "Repeat After Me only allows audio file.", 400);
                    }
                    else if (effectiveType == SpeakingExerciseType.PictureDescription || effectiveType == SpeakingExerciseType.StoryTelling)
                    {
                        if (files!.Any(f => !IsImage(f))) return BaseResponse<ExerciseResponse>.Fail(new { MediaFiles = "Format" }, "This type only allows image files.", 400);
                    }
                    else if (effectiveType == SpeakingExerciseType.Debate)
                    {
                        if (files!.Any(f => !IsImage(f))) return BaseResponse<ExerciseResponse>.Fail(new { MediaFiles = "Format" }, "Debate only allows image files.", 400);
                    }
                }

                // --- 5. TIẾN HÀNH UPLOAD VÀ CẬP NHẬT ---
                List<string> newMediaUrls = new List<string>();
                List<string> newMediaPublicIds = new List<string>();

                if (hasNewMedia)
                {
                    try
                    {
                        foreach (var mediaFile in files!)
                        {
                            if (mediaFile.Length == 0) continue;
                            UploadResultDto uploadResult;

                            // Upload Image or Audio based on check
                            if (IsImage(mediaFile))
                                uploadResult = await _cloudinary.UploadImageAsync(mediaFile, "exercises/images");
                            else
                                uploadResult = await _cloudinary.UploadAudioAsync(mediaFile, "exercises/audio");

                            if (uploadResult != null && !string.IsNullOrEmpty(uploadResult.Url))
                            {
                                newMediaUrls.Add(uploadResult.Url);
                                newMediaPublicIds.Add(uploadResult.PublicId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Rollback: Xóa file mới up nếu lỗi
                        foreach (var pid in newMediaPublicIds) await _cloudinary.DeleteFileAsync(pid);
                        return BaseResponse<ExerciseResponse>.Error($"Upload failed: {ex.Message}", 500);
                    }

                    // Xóa file CŨ trên Cloudinary (Clean up)
                    if (!string.IsNullOrEmpty(selectedExercise.MediaPublicId))
                    {
                        var oldIds = selectedExercise.MediaPublicId.Split(';', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var oldId in oldIds)
                        {
                            if (oldId != "null!") await _cloudinary.DeleteFileAsync(oldId);
                        }
                    }

                    // Gán file MỚI
                    selectedExercise.MediaUrl = string.Join(";", newMediaUrls);
                    selectedExercise.MediaPublicId = string.Join(";", newMediaPublicIds);
                }

                // Cập nhật các trường thông tin khác
                selectedExercise.Title = !string.IsNullOrWhiteSpace(request.Title) ? request.Title.Trim() : selectedExercise.Title;
                selectedExercise.Prompt = !string.IsNullOrWhiteSpace(request.Prompt) ? request.Prompt.Trim() : selectedExercise.Prompt;
                selectedExercise.Hints = !string.IsNullOrWhiteSpace(request.Hints) ? request.Hints.Trim() : selectedExercise.Hints;
                selectedExercise.Content = !string.IsNullOrWhiteSpace(request.Content) ? request.Content.Trim() : selectedExercise.Content;
                selectedExercise.ExpectedAnswer = !string.IsNullOrWhiteSpace(request.ExpectedAnswer) ? request.ExpectedAnswer.Trim() : selectedExercise.ExpectedAnswer;
                selectedExercise.FeedbackCorrect = !string.IsNullOrWhiteSpace(request.FeedbackCorrect) ? request.FeedbackCorrect.Trim() : selectedExercise.FeedbackCorrect;
                selectedExercise.FeedbackIncorrect = !string.IsNullOrWhiteSpace(request.FeedbackIncorrect) ? request.FeedbackIncorrect.Trim() : selectedExercise.FeedbackIncorrect;

                selectedExercise.Type = effectiveType;
                selectedExercise.Difficulty = request.Difficulty ?? selectedExercise.Difficulty;
                selectedExercise.MaxScore = request.MaxScore ?? selectedExercise.MaxScore;
                selectedExercise.PassScore = request.PassScore ?? selectedExercise.PassScore;
                selectedExercise.UpdatedAt = TimeHelper.GetVietnamTime();

                var updated = await _unit.Exercises.UpdateAsync(selectedExercise);
                if (updated <= 0) return BaseResponse<ExerciseResponse>.Fail("Failed to update exercise in DB.");

                if (selectedCourse.Status == CourseStatus.Rejected)
                {
                    selectedCourse.Status = CourseStatus.Draft;
                }

                await _unit.SaveChangesAsync();

                // Response mapping
                var response = new ExerciseResponse
                {
                    ExerciseID = selectedExercise.ExerciseID,
                    Title = selectedExercise.Title,
                    Prompt = selectedExercise.Prompt,
                    Hints = selectedExercise.Hints,
                    Content = selectedExercise.Content,
                    MediaUrls = selectedExercise.MediaUrl?.Split(';'),
                    MediaPublicIds = selectedExercise.MediaPublicId?.Split(';'),
                    ExpectedAnswer = selectedExercise.ExpectedAnswer,
                    Position = selectedExercise.Position,
                    ExerciseType = selectedExercise.Type.ToString(),
                    Difficulty = selectedExercise.Difficulty.ToString(),
                    MaxScore = selectedExercise.MaxScore,
                    PassScore = selectedExercise.PassScore,
                    FeedbackCorrect = selectedExercise.FeedbackCorrect,
                    FeedbackIncorrect = selectedExercise.FeedbackIncorrect,
                    CourseID = selectedCourse.CourseID,
                    CourseTitle = selectedCourse.Title,
                    UnitID = selectedUnit.CourseUnitID,
                    UnitTitle = selectedUnit.Title,
                    LessonID = selectedLesson.LessonID,
                    LessonTitle = selectedLesson.Title,
                    CreatedAt = selectedExercise.CreatedAt.ToString("dd-MM-yyyy"),
                    UpdatedAt = selectedExercise.UpdatedAt.ToString("dd-MM-yyyy")
                };

                return BaseResponse<ExerciseResponse>.Success(response, "Exercise updated successfully.", 200);
            }
            catch (Exception ex)
            {
                return BaseResponse<ExerciseResponse>.Error("An error occurred while updating exercise.", 500, ex.Message);
            }
        }
    }
}


