using BLL.IServices.ProgressTracking;
using Common.DTO.ApiResponse;
using Common.DTO.LessonProgress.Response;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.ProgressTracking
{
    public class LessonProgressService : ILessonProgressService
    {
        private readonly IUnitOfWork _unitOfWork;
        public LessonProgressService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<BaseResponse<LessonActivityStatusResponse>> GetLessonActivityStatusAsync(Guid userId, Guid lessonId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<LessonActivityStatusResponse>.Fail(new object(), "Access denied. Invalid authentication.", 401);

                var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
                if (lesson == null)
                    return BaseResponse<LessonActivityStatusResponse>.Fail(new object(), "Lesson not found", 404);

                var unit = await _unitOfWork.CourseUnits.GetByIdAsync(lesson.CourseUnitID);
                if (unit == null)
                    return BaseResponse<LessonActivityStatusResponse>.Fail(new object(), "Unit not found", 404);

                var course = await _unitOfWork.Courses.GetByIdAsync(unit.CourseID);
                if (course == null)
                    return BaseResponse<LessonActivityStatusResponse>.Fail(new object(), "Course not found", 404);

                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == user.UserID && l.LanguageId == course.LanguageId);
                if (learner == null)
                    return BaseResponse<LessonActivityStatusResponse>.Fail(new object(), "Learner not found", 403);

                var enrollment = await _unitOfWork.Enrollments.FindAsync(e => e.LearnerId == learner.LearnerLanguageId && e.CourseId == course.CourseID);
                if (enrollment == null)
                    return BaseResponse<LessonActivityStatusResponse>.Fail(new object(), "Enrollment not found", 404);

                var unitProgress = await _unitOfWork.UnitProgresses.FindAsync(up => up.EnrollmentId == enrollment.EnrollmentID && up.CourseUnitId == unit.CourseUnitID);
                if (unitProgress == null)
                    return BaseResponse<LessonActivityStatusResponse>.Fail(new object(), "Unit progress not found", 404);

                var lessonProgress = await _unitOfWork.LessonProgresses.Query()
                    .Include(lp => lp.LessonActivityLogs)
                    .Where(lp => lp.UnitProgressId == unitProgress.UnitProgressId && lp.LessonId == lesson.LessonID).FirstOrDefaultAsync();

                if (lessonProgress == null)
                    return BaseResponse<LessonActivityStatusResponse>.Fail(new object(), "Lesson progress not found", 404);

                var activityStatus = await GetLessonActivityStatus(lesson, lessonProgress, learner.LearnerLanguageId);

                return BaseResponse<LessonActivityStatusResponse>.Success(activityStatus, "Lesson activity status retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<LessonActivityStatusResponse>.Error($"Error retrieving lesson activity status: {ex.Message}");
            }
        }
        public async Task<BaseResponse<LessonProgressDetailResponse>> GetLessonProgressAsync(Guid userId, Guid lessonId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<LessonProgressDetailResponse>.Fail(new object(), "Access denied. Invalid authentication.", 401);

                var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
                if (lesson == null)
                    return BaseResponse<LessonProgressDetailResponse>.Fail(new object(), "Lesson not found", 404);

                var unit = await _unitOfWork.CourseUnits.GetByIdAsync(lesson.CourseUnitID);
                if (unit == null)
                    return BaseResponse<LessonProgressDetailResponse>.Fail(new object(), "Unit not found", 404);

                var course = await _unitOfWork.Courses.GetByIdAsync(unit.CourseID);
                if (course == null)
                    return BaseResponse<LessonProgressDetailResponse>.Fail(new object(), "Course not found", 404);

                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == user.UserID && l.LanguageId == course.LanguageId);
                if (learner == null)
                    return BaseResponse<LessonProgressDetailResponse>.Fail(new object(), "Learner not found", 403);

                var enrollment = await _unitOfWork.Enrollments.FindAsync(e => e.LearnerId == learner.LearnerLanguageId && e.CourseId == course.CourseID);
                if (enrollment == null)
                    return BaseResponse<LessonProgressDetailResponse>.Fail(new object(), "Enrollment not found", 404);

                var unitProgress = await _unitOfWork.UnitProgresses.FindAsync(up => up.EnrollmentId == enrollment.EnrollmentID && unit.CourseUnitID == up.CourseUnitId);
                if (unitProgress == null)
                    return BaseResponse<LessonProgressDetailResponse>.Fail(new object(), "Enrollment progress not found", 404);

                var lessonProgress = await _unitOfWork.LessonProgresses.Query()
                    .Include(lp => lp.UnitProgress)
                        .ThenInclude(up => up.Enrollment)
                    .Include(lp => lp.ExerciseSubmissions)
                        .ThenInclude(es => es.Exercise)
                    .Include(lp => lp.LessonActivityLogs)
                    .FirstOrDefaultAsync(lp => lp.LessonId == lesson.LessonID && lp.UnitProgressId == unitProgress.UnitProgressId);

                if (lessonProgress == null)
                    return BaseResponse<LessonProgressDetailResponse>.Fail(new object(), "Lesson progress not found", 404);

                var exercises = await GetExercisesWithSubmissions(lessonId, learner.LearnerLanguageId);

                var activityStatus = await GetLessonActivityStatus(lesson, lessonProgress, learner.LearnerLanguageId);

                var (previousLesson, nextLesson) = await GetNavigationLessons(lesson);

                var response = new LessonProgressDetailResponse
                {
                    LessonProgressId = lessonProgress?.LessonProgressId ?? Guid.Empty,
                    LessonId = lesson.LessonID,
                    LessonTitle = lesson.Title,
                    Description = lesson.Description,
                    ProgressPercent = lessonProgress?.ProgressPercent ?? 0,
                    Status = lessonProgress?.Status.ToString() ?? "NotStarted",
                    StartedAt = lessonProgress?.StartedAt.ToString("dd-MM-yyyy HH:mm") ?? string.Empty,
                    CompletedAt = lessonProgress?.CompletedAt?.ToString("dd-MM-yyyy HH:mm"),
                    LastUpdated = lessonProgress?.LastUpdated?.ToString("dd-MM-yyyy HH:mm"),
                    ActivityStatus = activityStatus,
                    TotalExercises = lesson.Exercises.Count,
                    CompletedExercises = exercises.Count(e => e.IsPassed == true),
                    PassedExercises = exercises.Count(e => e.IsPassed == true),
                    UnitId = unit.CourseUnitID,
                    UnitTitle = unit.Title,
                    CourseId = course.CourseID,
                    CourseTitle = course.Title,
                    PreviousLessonId = previousLesson?.LessonID,
                    PreviousLessonTitle = previousLesson?.Title,
                    NextLessonId = nextLesson?.LessonID,
                    NextLessonTitle = nextLesson?.Title,
                };

                return BaseResponse<LessonProgressDetailResponse>.Success(response, "Lesson progress retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<LessonProgressDetailResponse>.Error($"Error retrieving lesson progress: {ex.Message}");
            }
        }
        public async Task<BaseResponse<List<LessonProgressSummaryResponse>>> GetUnitLessonsProgressAsync(Guid userId, Guid unitId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<List<LessonProgressSummaryResponse>>.Fail(new object(), "Access denied. Invalid authentication.", 401);

                var unit = await _unitOfWork.CourseUnits.Query()
                    .Include(u => u.Course)
                    .Include(u => u.Lessons)
                        .ThenInclude(l => l.Exercises)
                    .FirstOrDefaultAsync(u => u.CourseUnitID == unitId);
                if (unit == null)
                    return BaseResponse<List<LessonProgressSummaryResponse>>.Fail(new object(), "Unit not found", 404);

                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == user.UserID && unit.Course != null && l.LanguageId == unit.Course.LanguageId);
                if (learner == null)
                    return BaseResponse<List<LessonProgressSummaryResponse>>.Fail(new object(), "Learner not found", 403);

                var enrollment = await _unitOfWork.Enrollments.FindAsync(e => e.LearnerId == learner.LearnerLanguageId && e.CourseId == unit.CourseID);
                if (enrollment == null)
                    return BaseResponse<List<LessonProgressSummaryResponse>>.Fail(new object(), "Enrollment not found", 404);

                var unitProgress = await _unitOfWork.UnitProgresses.FindAsync(up => up.EnrollmentId == enrollment.EnrollmentID && up.CourseUnitId == unit.CourseUnitID);
                if (unitProgress == null)
                    return BaseResponse<List<LessonProgressSummaryResponse>>.Fail(new object(), "Unit progress not found", 404);

                var lessonProgresses = await _unitOfWork.LessonProgresses.Query()
                    .Include(lp => lp.ExerciseSubmissions)
                    .Where(lp => lp.UnitProgressId == unitProgress.UnitProgressId)
                    .ToListAsync();

                var responses = new List<LessonProgressSummaryResponse>();

                foreach (var lesson in unit.Lessons.OrderBy(l => l.Position))
                {
                    var progress = lessonProgresses.FirstOrDefault(lp => lp.LessonId == lesson.LessonID);
                    var exercises = lesson.Exercises.ToList();
                    var submissions = progress?.ExerciseSubmissions ?? new List<ExerciseSubmission>();

                    responses.Add(new LessonProgressSummaryResponse
                    {
                        LessonId = lesson.LessonID,
                        Title = lesson.Title,
                        Order = lesson.Position,
                        ProgressPercent = progress?.ProgressPercent ?? 0,
                        Status = progress?.Status.ToString() ?? "NotStarted",
                        IsContentViewed = progress?.IsContentViewed ?? false,
                        IsVideoWatched = progress?.IsVideoWatched ?? false,
                        IsDocumentRead = progress?.IsDocumentRead ?? false,
                        IsPracticeCompleted = progress?.IsPracticeCompleted ?? false,
                        TotalExercises = exercises.Count,
                        CompletedExercises = submissions.Where(s => s.IsPassed == true).Select(s => s.ExerciseId).Distinct().Count(),
                    });
                }

                return BaseResponse<List<LessonProgressSummaryResponse>>.Success(responses, "Unit lessons progress retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<List<LessonProgressSummaryResponse>>.Error($"Error retrieving unit lessons progress: {ex.Message}");
            }
        }
        #region
        private async Task<LessonActivityStatusResponse> GetLessonActivityStatus(DAL.Models.Lesson lesson, LessonProgress? lessonProgress, Guid learnerId)
        {
            var activityLogs = lessonProgress?.LessonActivityLogs ?? new List<LessonActivityLog>();

            var contentLog = activityLogs.FirstOrDefault(l => l.ActivityType == LessonLogType.ContentRead);
            var videoLog = activityLogs.FirstOrDefault(l => l.ActivityType == LessonLogType.VideoProgress);
            var documentLog = activityLogs.FirstOrDefault(l => l.ActivityType == LessonLogType.PdfOpened);

            var hasContent = !string.IsNullOrEmpty(lesson.Content);
            var hasVideo = !string.IsNullOrEmpty(lesson.VideoUrl);
            var hasDocument = !string.IsNullOrEmpty(lesson.DocumentUrl);
            var hasExercises = await _unitOfWork.Exercises
                .Query()
                .AnyAsync(e => e.LessonID == lesson.LessonID);

            var exercises = await GetExercisesWithSubmissions(lesson.LessonID, learnerId);
            var allExercisesPassed = hasExercises && exercises.All(e => e.IsPassed == true);

            double progress = 0.0;
            var breakdown = new List<string>();

            if (hasContent && (lessonProgress?.IsContentViewed ?? false))
            {
                progress += 0.5;
                breakdown.Add("Content: 50%");
            }

            if (hasVideo && (lessonProgress?.IsVideoWatched ?? false))
            {
                progress += 0.2;
                breakdown.Add("Video: 20%");
            }

            if (hasDocument && (lessonProgress?.IsDocumentRead ?? false))
            {
                progress += 0.2;
                breakdown.Add("Document: 20%");
            }

            if (hasExercises && allExercisesPassed)
            {
                progress += 0.3;
                breakdown.Add("Exercises: 30%");
            }
            else if (hasExercises && exercises.Any(e => e.IsPassed == true))
            {
                var passedCount = exercises.Count(e => e.IsPassed == true);
                var exerciseProgress = 0.3 * passedCount / exercises.Count;
                progress += exerciseProgress;
                breakdown.Add($"Exercises: {exerciseProgress * 100}%");
            }

            progress = Math.Min(progress * 100, 100);

            var missingRequirements = new List<string>();

            if (hasContent && !(lessonProgress?.IsContentViewed ?? false))
                missingRequirements.Add("View lesson content");

            if (hasExercises && !allExercisesPassed)
                missingRequirements.Add("Complete all exercises");

            return new LessonActivityStatusResponse
            {
                LessonId = lesson.LessonID,
                LessonTitle = lesson.Title,

                IsContentViewed = lessonProgress?.IsContentViewed ?? false,
                IsVideoWatched = lessonProgress?.IsVideoWatched ?? false,
                IsDocumentRead = lessonProgress?.IsDocumentRead ?? false,
                IsPracticeCompleted = allExercisesPassed,

                Content = new ActivityDetail
                {
                    IsAvailable = hasContent,
                    IsCompleted = lessonProgress?.IsContentViewed ?? false,
                    CompletedAt = contentLog?.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                    ResourceUrl = lesson.Content,
                    ResourceTitle = "Lesson Content"
                },

                Video = new ActivityDetail
                {
                    IsAvailable = hasVideo,
                    IsCompleted = lessonProgress?.IsVideoWatched ?? false,
                    CompletedAt = videoLog?.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                    ResourceUrl = lesson.VideoUrl,
                    ResourceTitle = "Lesson Video"
                },

                Document = new ActivityDetail
                {
                    IsAvailable = hasDocument,
                    IsCompleted = lessonProgress?.IsDocumentRead ?? false,
                    CompletedAt = documentLog?.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                    ResourceUrl = lesson.DocumentUrl,
                    ResourceTitle = "Lesson Document"
                },

                CalculatedProgress = progress,
                ProgressBreakdown = string.Join(" + ", breakdown),
                MeetsCompletionRequirements = missingRequirements.Count == 0,
                MissingRequirements = missingRequirements
            };
        }
        private async Task<List<LessonExerciseProgressResponse>> GetExercisesWithSubmissions(Guid lessonId, Guid learnerId)
        {
            var exercises = await _unitOfWork.Exercises
                .Query()
                .Where(e => e.LessonID == lessonId)
                .OrderBy(e => e.Position)
                .ToListAsync();

            var submissions = await _unitOfWork.ExerciseSubmissions.Query()
                .Include(es => es.Exercise)
                .Include(es => es.LessonProgress)
                    .ThenInclude(lp => lp.UnitProgress)
                .Where(es => es.LearnerId == learnerId && es.Exercise.LessonID == lessonId)
                .ToListAsync();

            var result = new List<LessonExerciseProgressResponse>();

            foreach (var exercise in exercises)
            {
                var exerciseSubmissions = submissions
                    .Where(s => s.ExerciseId == exercise.ExerciseID)
                    .OrderByDescending(s => s.SubmittedAt)
                    .ToList();

                var latestSubmission = exerciseSubmissions.FirstOrDefault();
                var attemptCount = exerciseSubmissions.Count;

                result.Add(new LessonExerciseProgressResponse
                {
                    ExerciseId = exercise.ExerciseID,
                    Title = exercise.Title,
                    Type = exercise.Type.ToString(),
                    Description = exercise.Content,
                    PassScore = exercise.PassScore,
                    SubmissionId = latestSubmission?.ExerciseSubmissionId,
                    SubmissionStatus = latestSubmission?.Status.ToString() ?? "NotStarted",
                    Score = latestSubmission?.FinalScore ?? latestSubmission?.AIScore,
                    IsPassed = latestSubmission?.IsPassed,
                    SubmittedAt = latestSubmission?.SubmittedAt.ToString("dd-MM-yyyy HH:mm"),
                    ReviewedAt = latestSubmission?.ReviewedAt?.ToString("dd-MM-yyyy HH:mm"),
                    AIFeedback = latestSubmission?.AIFeedback,
                    TeacherFeedback = latestSubmission?.TeacherFeedback,
                    Order = exercise.Position,
                    IsCurrent = result.Count == 0
                });
            }
            return result;
        }
        private async Task<(DAL.Models.Lesson? previous, DAL.Models.Lesson? next)> GetNavigationLessons(DAL.Models.Lesson currentLesson)
        {
            var unitLessons = await _unitOfWork.Lessons
                .Query()
                .Where(l => l.CourseUnitID == currentLesson.CourseUnitID)
                .OrderBy(l => l.Position)
                .ToListAsync();

            var currentIndex = unitLessons.FindIndex(l => l.LessonID == currentLesson.LessonID);

            DAL.Models.Lesson? previous = currentIndex > 0 ? unitLessons[currentIndex - 1] : null;
            DAL.Models.Lesson? next = currentIndex < unitLessons.Count - 1 ? unitLessons[currentIndex + 1] : null;

            return (previous, next);
        }
        #endregion
    }
}
