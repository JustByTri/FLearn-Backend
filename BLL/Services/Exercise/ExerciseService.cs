using BLL.IServices.Exercise;
using BLL.IServices.Upload;
using BLL.Services.Exercise.Helper;
using Common.DTO.ApiResponse;
using Common.DTO.Exercise.Request;
using Common.DTO.Exercise.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.Models;
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

        public async Task<BaseResponse<ExerciseResponse>> CreateExerciseAsync(Guid teacherId, Guid courseId, Guid unitId, Guid lessonId, ExerciseRequest request)
        {
            var teacher = await _unit.Users.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == teacherId);

            if (teacher == null || !teacher.UserRoles.Any(ur => ur.Role.Name == "Teacher"))
            {
                return BaseResponse<ExerciseResponse>.Fail("Invalid TeacherID. Teacher not found or does not have role 'Teacher'.");
            }

            var course = await _unit.Courses.Query()
                .Include(c => c.CourseUnits)
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.TeacherID == teacherId);

            if (course == null)
            {
                return BaseResponse<ExerciseResponse>.Fail("Selected course not found or you are not the owner of this course.");
            }

            if (course.Status != CourseStatus.Draft && course.Status != CourseStatus.Rejected)
            {
                return BaseResponse<ExerciseResponse>.Fail("Only Draft or Rejected courses can be updated.");
            }

            var unit = await _unit.CourseUnits.Query()
                .Include(u => u.Lessons)
                .FirstOrDefaultAsync(u => u.CourseUnitID == unitId && u.CourseID == courseId);

            if (unit == null)
                return BaseResponse<ExerciseResponse>.Fail("Unit not found or not in course.");

            var lesson = await _unit.Lessons.Query()
                .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.LessonID == lessonId && l.CourseUnitID == unitId);

            if (lesson == null)
                return BaseResponse<ExerciseResponse>.Fail("Lesson not found or not in unit.");

            string? mediaUrl = null;
            string? mediaPublicId = null;

            try
            {
                if (request.MediaFile != null)
                {
                    var media = await _cloudinary.UploadAudioAsync(request.MediaFile, "exercises/media");
                    mediaUrl = media.Url;
                    mediaPublicId = media.PublicId;
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(mediaPublicId))
                    await _cloudinary.DeleteFileAsync(mediaPublicId);

                return BaseResponse<ExerciseResponse>.Error($"Error uploading media: {ex.Message}");
            }

            int nextPosition = lesson.Exercises.Any() ? lesson.Exercises.Max(e => e.Position) + 1 : 1;

            Guid? prerequisiteExerciseId = null;
            if (lesson.Exercises.Any())
            {
                var lastExercise = lesson.Exercises
                    .OrderByDescending(e => e.Position)
                    .FirstOrDefault();
                prerequisiteExerciseId = lastExercise?.ExerciseID;
            }

            var newExercise = new DAL.Models.Exercise
            {
                ExerciseID = Guid.NewGuid(),
                Title = request.Title,
                Prompt = request.Prompt,
                Hints = request.Hints,
                Content = request.Content,
                ExpectedAnswer = request.ExpectedAnswer,
                MediaUrl = mediaUrl,
                MediaPublicId = mediaPublicId,
                Position = nextPosition,
                Type = request.Type,
                SkillType = request.SkillType,
                Difficulty = request.Difficulty,
                MaxScore = request.MaxScore,
                PassScore = request.PassScore,
                FeedbackCorrect = request.FeedbackCorrect,
                FeedbackIncorrect = request.FeedbackIncorrect,
                LessonID = lesson.LessonID,
                PrerequisiteExerciseID = prerequisiteExerciseId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (request.Type == ExerciseType.ListeningMultipleChoice || request.Type == ExerciseType.UnscrambleWords)
            {
                if (request.Options == null || !request.Options.Any())
                {
                    return BaseResponse<ExerciseResponse>.Fail("Options are required for MultipleChoice and UnscrambleWords exercises.");
                }

                int nextOptionPosition = newExercise.Options.Any()
                    ? newExercise.Options.Max(o => o.Position) + 1
                    : 1;

                newExercise.Options = request.Options.Select((o, index) => new ExerciseOption
                {
                    OptionID = Guid.NewGuid(),
                    Text = o.Text,
                    IsCorrect = o.IsCorrect,
                    Position = nextOptionPosition + index
                }).ToList();
            }

            try
            {
                var result = await _unit.Exercises.CreateAsync(newExercise);
                if (result < 0)
                {
                    return BaseResponse<ExerciseResponse>.Fail("Failed when creating an exercise.");
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
                    MediaUrl = newExercise.MediaUrl,
                    MediaPublicId = newExercise.MediaPublicId,
                    Position = newExercise.Position,
                    ExerciseType = newExercise.Type.ToString(),
                    SkillType = newExercise.SkillType?.ToString(),
                    Difficulty = newExercise.Difficulty.ToString(),
                    MaxScore = newExercise.MaxScore,
                    PassScore = newExercise.PassScore,
                    FeedbackCorrect = newExercise.FeedbackCorrect,
                    FeedbackIncorrect = newExercise.FeedbackIncorrect,
                    PrerequisiteExerciseID = newExercise.PrerequisiteExerciseID,
                    LessonID = lesson.LessonID,
                    LessonTitle = lesson.Title,
                    UnitID = unit.CourseUnitID,
                    UnitTitle = unit.Title,
                    CourseID = course.CourseID,
                    CourseTitle = course.Title,
                    CreatedAt = newExercise.CreatedAt,
                    UpdatedAt = newExercise.UpdatedAt,
                    Options = newExercise.Options?.Select(o => new ExerciseOptionResponse
                    {
                        OptionID = o.OptionID,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect,
                        Position = o.Position
                    }).ToList(),
                };

                return BaseResponse<ExerciseResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return BaseResponse<ExerciseResponse>.Error($"Error: {ex.Message}");
            }
        }

        public async Task<BaseResponse<ExerciseResponse>> UpdateExerciseAsync(Guid teacherId, Guid courseId, Guid unitId, Guid lessonId, Guid exerciseId, ExerciseRequest request)
        {
            // Validate teacher
            var teacher = await _unit.Users.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == teacherId);

            if (teacher == null || !teacher.UserRoles.Any(ur => ur.Role.Name == "Teacher"))
                return BaseResponse<ExerciseResponse>.Fail("Invalid TeacherID. Teacher not found or not a Teacher.");

            // Validate course
            var course = await _unit.Courses.Query()
                .Include(c => c.CourseUnits)
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.TeacherID == teacherId);

            if (course == null)
                return BaseResponse<ExerciseResponse>.Fail("Course not found or not owned by this teacher.");

            if (course.Status != CourseStatus.Draft && course.Status != CourseStatus.Rejected)
                return BaseResponse<ExerciseResponse>.Fail("Only Draft or Rejected courses can be updated.");

            // Validate unit
            var unit = await _unit.CourseUnits.Query()
                .Include(u => u.Lessons)
                .FirstOrDefaultAsync(u => u.CourseUnitID == unitId && u.CourseID == courseId);

            if (unit == null)
                return BaseResponse<ExerciseResponse>.Fail("Unit not found or not in this course.");

            // Validate lesson
            var lesson = await _unit.Lessons.Query()
                .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.LessonID == lessonId && l.CourseUnitID == unitId);

            if (lesson == null)
                return BaseResponse<ExerciseResponse>.Fail("Lesson not found or not in this unit.");

            // Validate exercise
            var exercise = await _unit.Exercises.Query()
                .Include(e => e.Options)
                .FirstOrDefaultAsync(e => e.ExerciseID == exerciseId && e.LessonID == lessonId);

            if (exercise == null)
                return BaseResponse<ExerciseResponse>.Fail("Exercise not found.");

            // Handle media upload if updated
            string? mediaUrl = exercise.MediaUrl;
            string? mediaPublicId = exercise.MediaPublicId;

            try
            {
                if (request.MediaFile != null)
                {
                    // Delete old if exists
                    if (!string.IsNullOrEmpty(mediaPublicId))
                        await _cloudinary.DeleteFileAsync(mediaPublicId);

                    var media = await _cloudinary.UploadAudioAsync(request.MediaFile, "exercises/media");
                    mediaUrl = media.Url;
                    mediaPublicId = media.PublicId;
                }
            }
            catch (Exception ex)
            {
                return BaseResponse<ExerciseResponse>.Error($"Error uploading media: {ex.Message}");
            }

            // Update fields
            exercise.Title = request.Title;
            exercise.Prompt = request.Prompt;
            exercise.Hints = request.Hints;
            exercise.Content = request.Content;
            exercise.ExpectedAnswer = request.ExpectedAnswer;
            exercise.MediaUrl = mediaUrl;
            exercise.MediaPublicId = mediaPublicId;
            exercise.Type = request.Type;
            exercise.SkillType = request.SkillType;
            exercise.Difficulty = request.Difficulty;
            exercise.MaxScore = request.MaxScore;
            exercise.PassScore = request.PassScore;
            exercise.FeedbackCorrect = request.FeedbackCorrect;
            exercise.FeedbackIncorrect = request.FeedbackIncorrect;
            exercise.UpdatedAt = DateTime.UtcNow;

            // Update options if exercise type requires
            if (request.Type == ExerciseType.ListeningMultipleChoice || request.Type == ExerciseType.UnscrambleWords)
            {
                if (request.Options == null || !request.Options.Any())
                    return BaseResponse<ExerciseResponse>.Fail("Options are required for MultipleChoice and UnscrambleWords exercises.");

                await _unit.ExerciseOptions.RemoveRangeAsync(exercise.Options.ToList());

                int nextOptionPosition = exercise.Options.Any()
                   ? exercise.Options.Max(o => o.Position) + 1
                   : 1;

                exercise.Options = request.Options.Select((o, index) => new ExerciseOption
                {
                    OptionID = Guid.NewGuid(),
                    Text = o.Text,
                    IsCorrect = o.IsCorrect,
                    Position = nextOptionPosition + index
                }).ToList();
            }

            try
            {
                await _unit.Exercises.UpdateAsync(exercise);
                await _unit.SaveChangesAsync();

                var mapper = new MapToExerciseResponse();
                var response = mapper.MapToResponse(exercise, lesson, unit, course);
                return BaseResponse<ExerciseResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return BaseResponse<ExerciseResponse>.Error($"Error: {ex.Message}");
            }
        }

        public async Task<PagedResponse<IEnumerable<ExerciseResponse>>> GetExercisesByLessonIdAsync(
            Guid lessonId, PagingRequest request)
        {
            var query = _unit.Exercises.Query()
                .Include(e => e.Options)
                .Include(e => e.Lesson)
                    .ThenInclude(l => l.CourseUnit)
                    .ThenInclude(u => u.Course)
                .Where(e => e.LessonID == lessonId);

            var totalCount = await query.CountAsync();

            var exercises = await query
                .OrderBy(e => e.Position)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var mapper = new MapToExerciseResponse();
            var responseItems = exercises.Select(e =>
                mapper.MapToResponse(e, e.Lesson!, e.Lesson!.CourseUnit!, e.Lesson!.CourseUnit!.Course!));

            return PagedResponse<IEnumerable<ExerciseResponse>>.Success(
                responseItems,
                totalCount,
                request.Page,
                request.PageSize
            );
        }

        public async Task<BaseResponse<ExerciseResponse>> GetExerciseByIdAsync(Guid exerciseId)
        {
            var exercise = await _unit.Exercises.Query()
                .Include(e => e.Options)
                .Include(e => e.Lesson)
                    .ThenInclude(l => l.CourseUnit)
                    .ThenInclude(u => u.Course)
                .FirstOrDefaultAsync(e => e.ExerciseID == exerciseId);

            if (exercise == null)
                return BaseResponse<ExerciseResponse>.Fail("Exercise not found.");

            var mapper = new MapToExerciseResponse();
            var response = mapper.MapToResponse(exercise, exercise.Lesson!, exercise.Lesson!.CourseUnit!, exercise.Lesson!.CourseUnit!.Course!);
            return BaseResponse<ExerciseResponse>.Success(response);
        }

        public async Task<BaseResponse<ExerciseResponse>> DeleteExerciseAsync(
            Guid teacherId, Guid courseId, Guid unitId, Guid lessonId, Guid exerciseId)
        {
            // Validate teacher
            var teacher = await _unit.Users.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == teacherId);

            if (teacher == null || !teacher.UserRoles.Any(ur => ur.Role.Name == "Teacher"))
                return BaseResponse<ExerciseResponse>.Fail("Invalid TeacherID. Teacher not found or not a Teacher.");

            // Validate course
            var course = await _unit.Courses.Query()
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.TeacherID == teacherId);

            if (course == null)
                return BaseResponse<ExerciseResponse>.Fail("Course not found or not owned by this teacher.");

            if (course.Status != CourseStatus.Draft && course.Status != CourseStatus.Rejected)
                return BaseResponse<ExerciseResponse>.Fail("Only Draft or Rejected courses can be updated.");

            // Validate unit
            var unit = await _unit.CourseUnits.Query()
                .FirstOrDefaultAsync(u => u.CourseUnitID == unitId && u.CourseID == courseId);

            if (unit == null)
                return BaseResponse<ExerciseResponse>.Fail("Unit not found or not in this course.");

            // Validate lesson
            var lesson = await _unit.Lessons.Query()
                .FirstOrDefaultAsync(l => l.LessonID == lessonId && l.CourseUnitID == unitId);

            if (lesson == null)
                return BaseResponse<ExerciseResponse>.Fail("Lesson not found or not in this unit.");

            // Validate exercise
            var exercise = await _unit.Exercises.Query()
                .Include(e => e.Options)
                .FirstOrDefaultAsync(e => e.ExerciseID == exerciseId && e.LessonID == lessonId);

            if (exercise == null)
                return BaseResponse<ExerciseResponse>.Fail("Exercise not found.");

            try
            {
                // Delete media if exists
                if (!string.IsNullOrEmpty(exercise.MediaPublicId))
                    await _cloudinary.DeleteFileAsync(exercise.MediaPublicId);

                await _unit.Exercises.RemoveAsync(exercise);
                await _unit.SaveChangesAsync();

                var mapper = new MapToExerciseResponse();
                var response = mapper.MapToResponse(exercise, lesson, unit, course);
                return BaseResponse<ExerciseResponse>.Success(response, "Exercise deleted successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<ExerciseResponse>.Error($"Error: {ex.Message}");
            }
        }
    }
}
