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
        public async Task<BaseResponse<ExerciseResponse>> CreateExerciseAsync(Guid userId, Guid lessonId, ExerciseRequest request)
        {
            try
            {
                var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
                if (teacher == null)
                    return BaseResponse<ExerciseResponse>.Fail(new object(), "Access denied", 403);

                var selectedLesson = await _unit.Lessons.GetByIdAsync(lessonId);
                if (selectedLesson == null)
                {
                    return BaseResponse<ExerciseResponse>.Fail("Lesson not found.");
                }

                var selectedUnit = await _unit.CourseUnits.GetByIdAsync(selectedLesson.CourseUnitID);
                if (selectedUnit == null)
                {
                    return BaseResponse<ExerciseResponse>.Fail("Course unit not found.");
                }

                var selectedCourse = await _unit.Courses.GetByIdAsync(selectedUnit.CourseID);
                if (selectedCourse == null)
                {
                    return BaseResponse<ExerciseResponse>.Fail("Course not found.");
                }

                if (selectedCourse.Status != CourseStatus.Draft && selectedCourse.Status != CourseStatus.Rejected)
                {
                    return BaseResponse<ExerciseResponse>.Fail(
                        new { CourseStatus = "Invalid course status." },
                        "Only Draft or Rejected courses can be modified.",
                        400
                    );
                }

                if (request.PassScore > request.MaxScore)
                {
                    return BaseResponse<ExerciseResponse>.Fail(
                        new { PassScore = "Pass score cannot exceed max score." },
                        "Invalid score configuration.",
                        400
                    );
                }

                var mediaUrls = new List<string>();
                var mediaPublicIds = new List<string>();

                try
                {
                    if (request.MediaFiles != null && request.MediaFiles.Any())
                    {
                        foreach (var mediaFile in request.MediaFiles)
                        {
                            if (mediaFile.Length == 0)
                                continue;

                            var contentType = mediaFile.ContentType.ToLower();
                            bool isImage = contentType.StartsWith("image/");
                            bool isAudio = contentType.StartsWith("audio/");

                            if (!isImage && !isAudio)
                            {
                                foreach (var publicId in mediaPublicIds)
                                {
                                    await _cloudinary.DeleteFileAsync(publicId);
                                }

                                return BaseResponse<ExerciseResponse>.Fail(
                                    new { MediaFiles = "Invalid file format." },
                                    "Only image or audio files are allowed.",
                                    400
                                );
                            }

                            UploadResultDto uploadResult;

                            if (isImage)
                            {
                                uploadResult = await _cloudinary.UploadImageAsync(mediaFile, "exercises/images");
                            }
                            else
                            {
                                uploadResult = await _cloudinary.UploadAudioAsync(mediaFile, "exercises/audio");
                            }

                            if (uploadResult != null && !string.IsNullOrEmpty(uploadResult.Url))
                            {
                                mediaUrls.Add(uploadResult.Url);
                                mediaPublicIds.Add(uploadResult.PublicId);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    foreach (var publicId in mediaPublicIds)
                    {
                        await _cloudinary.DeleteFileAsync(publicId);
                    }

                    return BaseResponse<ExerciseResponse>.Error($"Upload file failed: {ex.Message}", 500);
                }

                var existingExercises = await _unit.Exercises.FindAllAsync(ex => ex.LessonID == selectedLesson.LessonID);
                int nextPosition = existingExercises.Any() ? existingExercises.Max(e => e.Position) + 1 : 1;

                selectedLesson.TotalExercises += 1;
                _unit.Lessons.Update(selectedLesson);

                var now = TimeHelper.GetVietnamTime();

                var newExercise = new DAL.Models.Exercise
                {
                    ExerciseID = Guid.NewGuid(),
                    Title = request.Title.Trim(),
                    Prompt = string.IsNullOrWhiteSpace(request.Prompt) ? null : request.Prompt.Trim(),
                    Hints = string.IsNullOrWhiteSpace(request.Hints) ? null : request.Hints.Trim(),
                    Content = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content.Trim(),
                    ExpectedAnswer = string.IsNullOrWhiteSpace(request.ExpectedAnswer) ? null : request.ExpectedAnswer.Trim(),
                    Type = request.Type,
                    Difficulty = request.Difficulty,
                    Position = nextPosition,
                    MaxScore = request.MaxScore,
                    PassScore = request.PassScore,
                    FeedbackCorrect = string.IsNullOrWhiteSpace(request.FeedbackCorrect) ? null : request.FeedbackCorrect.Trim(),
                    FeedbackIncorrect = string.IsNullOrWhiteSpace(request.FeedbackIncorrect) ? null : request.FeedbackIncorrect.Trim(),
                    LessonID = selectedLesson.LessonID,
                    MediaUrl = mediaUrls.Any() ? string.Join(";", mediaUrls) : null,
                    MediaPublicId = mediaPublicIds.Any() ? string.Join(";", mediaPublicIds) : null,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _unit.Exercises.CreateAsync(newExercise);
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
                    return BaseResponse<ExerciseResponse>.Fail("Teacher does not exist.");

                var exercise = await _unit.Exercises.Query()
                    .Include(e => e.Lesson)
                        .ThenInclude(l => l.CourseUnit)
                        .ThenInclude(u => u.Course)
                    .FirstOrDefaultAsync(e => e.ExerciseID == exerciseId);

                if (exercise == null)
                    return BaseResponse<ExerciseResponse>.Fail(null, "Exercise not found.", 404);

                var course = exercise.Lesson.CourseUnit.Course;
                if (course.TeacherId != teacher.TeacherId)
                    return BaseResponse<ExerciseResponse>.Fail(
                        null,
                        "You do not have permission to delete this exercise.",
                        403
                    );

                if (course.Status != CourseStatus.Draft && course.Status != CourseStatus.Rejected)
                    return BaseResponse<ExerciseResponse>.Fail(
                        new { CourseStatus = "Invalid course status." },
                        "Only Draft or Rejected courses can be deleted.",
                        400
                    );

                // Delete media if exists
                if (!string.IsNullOrEmpty(exercise.MediaPublicId))
                    await _cloudinary.DeleteFileAsync(exercise.MediaPublicId);

                exercise.Lesson.TotalExercises -= 1;

                var deleted = await _unit.Exercises.RemoveAsync(exercise);
                if (!deleted)
                    return BaseResponse<ExerciseResponse>.Fail("Failed to delete exercise.");

                await _unit.SaveChangesAsync();

                return BaseResponse<ExerciseResponse>.Success(null, "Exercise deleted successfully.", 200);
            }
            catch (Exception ex)
            {
                return BaseResponse<ExerciseResponse>.Error("An error occurred while deleting exercise.", 500, ex.Message);
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
                var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
                if (teacher == null)
                    return BaseResponse<ExerciseResponse>.Fail("Teacher does not exist.");

                var selectedLesson = await _unit.Lessons.Query()
                    .Include(l => l.CourseUnit)
                        .ThenInclude(u => u.Course)
                    .Include(l => l.Exercises)
                    .FirstOrDefaultAsync(l => l.LessonID == lessonId);

                if (selectedLesson == null)
                {
                    return BaseResponse<ExerciseResponse>.Fail(
                        new { Lesson = "Lesson not found." },
                        "Lesson does not exist.",
                        404
                    );
                }

                var selectedUnit = selectedLesson.CourseUnit;
                if (selectedUnit == null)
                {
                    return BaseResponse<ExerciseResponse>.Fail(
                        new { Unit = "Unit not found for this lesson." },
                        "Invalid lesson reference.",
                        404
                    );
                }

                var selectedCourse = selectedUnit.Course;
                if (selectedCourse == null)
                {
                    return BaseResponse<ExerciseResponse>.Fail(
                        new { Course = "Course not found for this unit." },
                        "Invalid course reference.",
                        404
                    );
                }

                if (selectedCourse.TeacherId != teacher.TeacherId)
                {
                    return BaseResponse<ExerciseResponse>.Fail(null,
                        "You do not have permission to create an exercise for this lesson because you are not the owner of this course.",
                        403
                    );
                }

                if (selectedCourse.Status != CourseStatus.Draft && selectedCourse.Status != CourseStatus.Rejected)
                {
                    return BaseResponse<ExerciseResponse>.Fail(
                        new { CourseStatus = "Invalid course status." },
                        "Only Draft or Rejected courses can be modified.",
                        400
                    );
                }

                var selectedExercise = selectedLesson.Exercises.FirstOrDefault(e => e.ExerciseID == exerciseId);
                if (selectedExercise == null)
                    return BaseResponse<ExerciseResponse>.Fail(
                        new { ExerciseId = "Exercise not found." },
                        "Exercise does not exist.",
                        404
                    );

                string? newMediaUrl = null;
                string? newMediaPublicId = null;

                try
                {
                    if (request.MediaFile != null)
                    {
                        var contentType = request.MediaFile.ContentType.ToLower();

                        if (!contentType.StartsWith("audio/"))
                        {
                            return BaseResponse<ExerciseResponse>.Fail(
                                new { MediaFile = "Invalid file format." },
                                "Only audio is allowed.",
                                400
                            );
                        }

                        // Delete old file if exists
                        if (!string.IsNullOrEmpty(selectedExercise.MediaPublicId))
                            await _cloudinary.DeleteFileAsync(selectedExercise.MediaPublicId);

                        // Upload new file
                        var uploaded = await _cloudinary.UploadAudioAsync(request.MediaFile, "exercises/media");
                        newMediaUrl = uploaded.Url;
                        newMediaPublicId = uploaded.PublicId;
                    }
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(newMediaPublicId))
                        await _cloudinary.DeleteFileAsync(newMediaPublicId);

                    return BaseResponse<ExerciseResponse>.Error($"Upload media failed: {ex.Message}", 500);
                }

                selectedExercise.Title = !string.IsNullOrWhiteSpace(request.Title) ? request.Title.Trim() : selectedExercise.Title;
                selectedExercise.Prompt = !string.IsNullOrWhiteSpace(request.Prompt) ? request.Prompt.Trim() : selectedExercise.Prompt;
                selectedExercise.Hints = !string.IsNullOrWhiteSpace(request.Hints) ? request.Hints.Trim() : selectedExercise.Hints;
                selectedExercise.Content = !string.IsNullOrWhiteSpace(request.Content) ? request.Content.Trim() : selectedExercise.Content;
                selectedExercise.ExpectedAnswer = !string.IsNullOrWhiteSpace(request.ExpectedAnswer) ? request.ExpectedAnswer.Trim() : selectedExercise.ExpectedAnswer;
                selectedExercise.FeedbackCorrect = !string.IsNullOrWhiteSpace(request.FeedbackCorrect) ? request.FeedbackCorrect.Trim() : selectedExercise.FeedbackCorrect;
                selectedExercise.FeedbackIncorrect = !string.IsNullOrWhiteSpace(request.FeedbackIncorrect) ? request.FeedbackIncorrect.Trim() : selectedExercise.FeedbackIncorrect;

                selectedExercise.Type = request.Type ?? selectedExercise.Type;
                selectedExercise.Difficulty = request.Difficulty ?? selectedExercise.Difficulty;
                selectedExercise.MaxScore = request.MaxScore ?? selectedExercise.MaxScore;
                selectedExercise.PassScore = request.PassScore ?? selectedExercise.PassScore;

                if (newMediaUrl != null)
                {
                    selectedExercise.MediaUrl = newMediaUrl;
                    selectedExercise.MediaPublicId = newMediaPublicId;
                }

                selectedExercise.UpdatedAt = TimeHelper.GetVietnamTime();

                var updated = await _unit.Exercises.UpdateAsync(selectedExercise);
                if (updated <= 0)
                    return BaseResponse<ExerciseResponse>.Fail("Failed to update exercise.");

                await _unit.SaveChangesAsync();

                var response = new ExerciseResponse
                {
                    ExerciseID = selectedExercise.ExerciseID,
                    Title = selectedExercise.Title,
                    Prompt = selectedExercise.Prompt,
                    Hints = selectedExercise.Hints,
                    Content = selectedExercise.Content,
                    MediaUrls = selectedExercise.MediaUrl != null ? selectedExercise.MediaUrl.Split(';') : null,
                    MediaPublicIds = selectedExercise.MediaPublicId != null ? selectedExercise.MediaPublicId.Split(';') : null,
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
