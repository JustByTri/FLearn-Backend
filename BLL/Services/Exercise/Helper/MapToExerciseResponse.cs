using Common.DTO.Exercise.Response;
using DAL.Models;

namespace BLL.Services.Exercise.Helper
{
    public class MapToExerciseResponse
    {
        public ExerciseResponse MapToResponse(DAL.Models.Exercise e, Lesson lesson, CourseUnit unit, Course course)
        {
            return new ExerciseResponse
            {
                ExerciseID = e.ExerciseID,
                Title = e.Title,
                Prompt = e.Prompt,
                Hints = e.Hints,
                Content = e.Content,
                ExpectedAnswer = e.ExpectedAnswer,
                MediaUrl = e.MediaUrl,
                MediaPublicId = e.MediaPublicId,
                Position = e.Position,
                ExerciseType = e.Type.ToString(),
                SkillType = e.SkillType?.ToString(),
                Difficulty = e.Difficulty.ToString(),
                MaxScore = e.MaxScore,
                PassScore = e.PassScore,
                FeedbackCorrect = e.FeedbackCorrect,
                FeedbackIncorrect = e.FeedbackIncorrect,
                PrerequisiteExerciseID = e.PrerequisiteExerciseID,
                LessonID = lesson.LessonID,
                LessonTitle = lesson.Title,
                UnitID = unit.CourseUnitID,
                UnitTitle = unit.Title,
                CourseID = course.CourseID,
                CourseTitle = course.Title,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                Options = e.Options?.Select(o => new ExerciseOptionResponse
                {
                    OptionID = o.OptionID,
                    Text = o.Text,
                    IsCorrect = o.IsCorrect,
                    Position = o.Position
                }).ToList(),
            };
        }
    }
}
