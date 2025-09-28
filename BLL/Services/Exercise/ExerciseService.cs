using BLL.IServices.Exercise;
using BLL.IServices.Upload;
using Common.DTO.ApiResponse;
using Common.DTO.Exercise.Request;
using Common.DTO.Exercise.Response;
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
                .FirstOrDefaultAsync(c => c.CourseID == courseId);

            if (course == null)
            {
                return BaseResponse<ExerciseResponse>.Fail("Selected course not found.");
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
    }
}
