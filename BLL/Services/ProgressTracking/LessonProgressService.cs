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

                int passedCount = exercises.Count(e => e.IsPassed == true);
                int totalCount = lesson.Exercises.Count;

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
                    IsAllExercisesPassed = totalCount > 0 && passedCount == totalCount,
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
        public async Task<BaseResponse<List<LessonExerciseProgressResponse>>> GetLessonExercisesWithStatusAsync(Guid userId, Guid lessonId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null) return BaseResponse<List<LessonExerciseProgressResponse>>.Fail(new object(), "Invalid user", 401);

                var lesson = await _unitOfWork.Lessons.Query()
                    .Include(l => l.CourseUnit).ThenInclude(u => u.Course)
                    .FirstOrDefaultAsync(l => l.LessonID == lessonId);

                if (lesson == null) return BaseResponse<List<LessonExerciseProgressResponse>>.Fail(new object(), "Lesson not found", 404);

                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == user.UserID && l.LanguageId == lesson.CourseUnit.Course.LanguageId);
                if (learner == null) return BaseResponse<List<LessonExerciseProgressResponse>>.Fail(new object(), "Learner profile not found", 403);

                var exercisesWithStatus = await GetExercisesWithSubmissions(lessonId, learner.LearnerLanguageId);

                return BaseResponse<List<LessonExerciseProgressResponse>>.Success(exercisesWithStatus, "Exercises retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<List<LessonExerciseProgressResponse>>.Error($"Error retrieving exercises: {ex.Message}");
            }
        }
        #region
        private async Task<LessonActivityStatusResponse> GetLessonActivityStatus(DAL.Models.Lesson lesson, LessonProgress? lessonProgress, Guid learnerId)
        {
            var activityLogs = lessonProgress?.LessonActivityLogs ?? new List<LessonActivityLog>();

            var contentLog = activityLogs.FirstOrDefault(l => l.ActivityType == LessonLogType.ContentRead);
            var videoLog = activityLogs.FirstOrDefault(l => l.ActivityType == LessonLogType.VideoProgress);
            var documentLog = activityLogs.FirstOrDefault(l => l.ActivityType == LessonLogType.PdfOpened);

            // Xác định các thành phần có trong bài học
            var hasContent = true; // Theo logic UpdateOverallLessonProgress, Content luôn tính là 1 part
            var hasVideo = !string.IsNullOrEmpty(lesson.VideoUrl);
            var hasDocument = !string.IsNullOrEmpty(lesson.DocumentUrl);

            // Lấy danh sách bài tập và tính toán trạng thái
            var exercises = await GetExercisesWithSubmissions(lesson.LessonID, learnerId);
            var hasExercises = exercises.Any();

            // Logic tính toán Progress y hệt UpdateOverallLessonProgress
            double totalProgress = 0.0;
            int totalParts = 0;

            // 1. Phần Content
            totalParts++; // Luôn cộng 1 part cho content
            bool isContentDone = lessonProgress?.IsContentViewed ?? false;
            if (isContentDone)
            {
                totalProgress += 1.0;
            }

            // 2. Phần Video
            bool isVideoDone = false;
            if (hasVideo)
            {
                totalParts++;
                isVideoDone = lessonProgress?.IsVideoWatched ?? false;
                if (isVideoDone)
                {
                    totalProgress += 1.0;
                }
            }

            // 3. Phần Document
            bool isDocumentDone = false;
            if (hasDocument)
            {
                totalParts++;
                isDocumentDone = lessonProgress?.IsDocumentRead ?? false;
                if (isDocumentDone)
                {
                    totalProgress += 1.0;
                }
            }

            bool allExercisesPassed = false;
            double currentExercisePercent = 0.0;

            if (hasExercises)
            {
                totalParts++;
                int totalEx = exercises.Count;
                int passedCount = exercises.Count(e => e.IsPassed == true);

                double exerciseRatio = totalEx > 0 ? (double)passedCount / totalEx : 0;

                totalProgress += exerciseRatio;
                currentExercisePercent = exerciseRatio * 100;

                if (passedCount == totalEx && totalEx > 0)
                {
                    allExercisesPassed = true;
                }
            }
            else
            {
                allExercisesPassed = true;
            }

            double finalProgressPercent = totalParts > 0 ? (totalProgress / totalParts) * 100 : 0;
            finalProgressPercent = Math.Min(finalProgressPercent, 100);

            var breakdown = new List<string>();
            double weightPerPart = totalParts > 0 ? 100.0 / totalParts : 0;

            breakdown.Add($"Content: {(isContentDone ? weightPerPart : 0):F0}%/{weightPerPart:F0}%");

            if (hasVideo)
                breakdown.Add($"Video: {(isVideoDone ? weightPerPart : 0):F0}%/{weightPerPart:F0}%");

            if (hasDocument)
                breakdown.Add($"Document: {(isDocumentDone ? weightPerPart : 0):F0}%/{weightPerPart:F0}%");

            if (hasExercises)
            {
                double earnedFromEx = (currentExercisePercent / 100.0) * weightPerPart;
                breakdown.Add($"Exercises: {earnedFromEx:F0}%/{weightPerPart:F0}%");
            }

            var missingRequirements = new List<string>();
            if (!(lessonProgress?.IsContentViewed ?? false)) missingRequirements.Add("View lesson content");
            if (hasExercises && !allExercisesPassed) missingRequirements.Add("Complete all exercises");

            return new LessonActivityStatusResponse
            {
                LessonId = lesson.LessonID,
                LessonTitle = lesson.Title,

                IsContentViewed = isContentDone,
                IsVideoWatched = isVideoDone,
                IsDocumentRead = isDocumentDone,
                IsPracticeCompleted = allExercisesPassed,

                Content = new ActivityDetail
                {
                    IsAvailable = hasContent,
                    IsCompleted = isContentDone,
                    CompletedAt = contentLog?.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                    ResourceUrl = lesson.Content,
                    ResourceTitle = "Lesson Content"
                },

                Video = new ActivityDetail
                {
                    IsAvailable = hasVideo,
                    IsCompleted = isVideoDone,
                    CompletedAt = videoLog?.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                    ResourceUrl = lesson.VideoUrl,
                    ResourceTitle = "Lesson Video"
                },

                Document = new ActivityDetail
                {
                    IsAvailable = hasDocument,
                    IsCompleted = isDocumentDone,
                    CompletedAt = documentLog?.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                    ResourceUrl = lesson.DocumentUrl,
                    ResourceTitle = "Lesson Document"
                },

                CalculatedProgress = finalProgressPercent,
                ProgressBreakdown = string.Join(" + ", breakdown),
                MeetsCompletionRequirements = missingRequirements.Count == 0,
                MissingRequirements = missingRequirements
            };
        }
        private async Task<List<LessonExerciseProgressResponse>> GetExercisesWithSubmissions(Guid lessonId, Guid learnerId)
        {
            var exercises = await _unitOfWork.Exercises
                            .Query()
                            .Include(e => e.Lesson)
                                .ThenInclude(l => l.CourseUnit)
                                    .ThenInclude(u => u.Course)
                            .Where(e => e.LessonID == lessonId)
                            .OrderBy(e => e.Position)
                            .ToListAsync();

            var submissions = await _unitOfWork.ExerciseSubmissions.Query()
                            .Include(es => es.Exercise)
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

                bool hasPassedAnyTime = exerciseSubmissions.Any(s => s.IsPassed == true);

                result.Add(new LessonExerciseProgressResponse
                {
                    ExerciseID = exercise.ExerciseID,
                    Title = exercise.Title,
                    Prompt = exercise.Prompt,
                    Hints = exercise.Hints,
                    Content = exercise.Content,
                    ExpectedAnswer = exercise.ExpectedAnswer,

                    MediaUrls = exercise.MediaUrl != null
                        ? exercise.MediaUrl.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        : null,
                    MediaPublicIds = exercise.MediaPublicId != null
                        ? exercise.MediaPublicId.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        : null,

                    Position = exercise.Position,
                    ExerciseType = exercise.Type.ToString(),
                    Difficulty = exercise.Difficulty.ToString(),
                    MaxScore = exercise.MaxScore,
                    PassScore = exercise.PassScore,
                    FeedbackCorrect = exercise.FeedbackCorrect,
                    FeedbackIncorrect = exercise.FeedbackIncorrect,

                    LessonID = exercise.LessonID,
                    LessonTitle = exercise.Lesson?.Title,
                    UnitID = exercise.Lesson?.CourseUnitID,
                    UnitTitle = exercise.Lesson?.CourseUnit?.Title,
                    CourseID = exercise.Lesson?.CourseUnit?.CourseID,
                    CourseTitle = exercise.Lesson?.CourseUnit?.Course?.Title,

                    SubmissionId = latestSubmission?.ExerciseSubmissionId,
                    SubmissionStatus = latestSubmission?.Status.ToString() ?? "NotStarted",
                    Score = latestSubmission?.FinalScore ?? latestSubmission?.AIScore,
                    IsPassed = hasPassedAnyTime,
                    SubmittedAt = latestSubmission?.SubmittedAt.ToString("dd-MM-yyyy HH:mm"),
                    ReviewedAt = latestSubmission?.ReviewedAt?.ToString("dd-MM-yyyy HH:mm"),
                    AIFeedback = latestSubmission?.AIFeedback,
                    TeacherFeedback = latestSubmission?.TeacherFeedback,

                    IsCurrent = false
                });
            }

            var firstNotPassed = result.FirstOrDefault(x => x.IsPassed != true);
            if (firstNotPassed != null)
            {
                firstNotPassed.IsCurrent = true;
            }
            else if (result.Count > 0)
            {
                result.Last().IsCurrent = true;
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
