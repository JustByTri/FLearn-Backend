using Azure.Core;
using BLL.IServices.ProgressTracking;
using BLL.IServices.Upload;
using BLL.IServices.Gamification;
using Common.DTO.ApiResponse;
using Common.DTO.Assement;
using Common.DTO.ExerciseGrading.Request;
using Common.DTO.ExerciseSubmission.Response;
using Common.DTO.Paging.Response;
using Common.DTO.ProgressTracking.Request;
using Common.DTO.ProgressTracking.Response;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace BLL.Services.ProgressTracking
{
    public class ProgressTrackingService : IProgressTrackingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IExerciseGradingService _exerciseGradingService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IGamificationService _gamificationService;
        private const double DefaultAIPercentage = 30;
        private const double DefaultTeacherPercentage = 70;
        public ProgressTrackingService(IUnitOfWork unitOfWork, IExerciseGradingService exerciseGradingService, ICloudinaryService cloudinaryService, IGamificationService gamificationService)
        {
            _unitOfWork = unitOfWork;
            _exerciseGradingService = exerciseGradingService;
            _cloudinaryService = cloudinaryService;
            _gamificationService = gamificationService;
        }
        public async Task<PagedResponse<List<ExerciseSubmissionDetailResponse>>> GetMySubmissionsAsync(Guid userId, Guid courseId, Guid lessonId, string? status, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return PagedResponse<List<ExerciseSubmissionDetailResponse>>.Fail(new object(), "Access denied. Invalid authentication.", 401);

                var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
                if (course == null)
                    return PagedResponse<List<ExerciseSubmissionDetailResponse>>.Fail(new object(), "Course not found.", 404);

                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == userId && l.LanguageId == course.LanguageId);
                if (learner == null)
                    return PagedResponse<List<ExerciseSubmissionDetailResponse>>.Fail(new object(), "Learner not found", 403);

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var query = _unitOfWork.ExerciseSubmissions.Query()
                    .Where(es => es.LearnerId == learner.LearnerLanguageId)
                    .Include(es => es.Exercise)
                        .ThenInclude(e => e.Lesson)
                            .ThenInclude(l => l.CourseUnit)
                                .ThenInclude(u => u.Course)
                    .Include(es => es.ExerciseGradingAssignments)
                    .ThenInclude(ega => ega.Teacher)
                    .ThenInclude(t => t.User)
                    .AsQueryable();
                
                query = query.Where(es => es.Exercise != null && es.Exercise.Lesson != null && es.Exercise.Lesson.CourseUnit != null && es.Exercise.Lesson.CourseUnit.CourseID == course.CourseID);
                query = query.Where(es => es.Exercise.LessonID == lessonId);

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<ExerciseSubmissionStatus>(status, out var statusFilter))
                {
                    query = query.Where(es => es.Status == statusFilter);
                }

                var totalCount = await query.CountAsync();

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var submissions = await query
                    .OrderByDescending(es => es.SubmittedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var responses = submissions.Select(es => MapToDetailResponse(es)).ToList();

                return PagedResponse<List<ExerciseSubmissionDetailResponse>>.Success(
                    responses,
                    pageNumber,
                    pageSize,
                    totalCount,
                    "Submissions retrieved successfully",
                    200);
            }
            catch (Exception ex)
            {
                return PagedResponse<List<ExerciseSubmissionDetailResponse>>.Error($"Error retrieving submissions: {ex.Message}");
            }
        }
        public async Task<BaseResponse<ExerciseSubmissionDetailResponse>> GetSubmissionDetailAsync(Guid userId, Guid submissionId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<ExerciseSubmissionDetailResponse>.Fail(new object(), "Access denied. Invalid authentication.", 401);

                var submission = await _unitOfWork.ExerciseSubmissions.Query()
                    .Include(es => es.Exercise)
                        .ThenInclude(e => e.Lesson)
                            .ThenInclude(l => l.CourseUnit)
                                .ThenInclude(u => u.Course)
                    .Include(es => es.ExerciseGradingAssignments)
                    .ThenInclude(ega => ega.Teacher)
                    .ThenInclude(t => t.User)
                    .Include(es => es.LessonProgress)
                    .FirstOrDefaultAsync(es => es.ExerciseSubmissionId == submissionId);

                if (submission == null)
                    return BaseResponse<ExerciseSubmissionDetailResponse>.Fail(new ExerciseSubmissionDetailResponse(), "Submission not found", 404);

                var response = MapToDetailResponse(submission);

                return BaseResponse<ExerciseSubmissionDetailResponse>.Success(response, "Submission detail retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<ExerciseSubmissionDetailResponse>.Error($"Error retrieving submission detail: {ex.Message}");
            }
        }
        public async Task<PagedResponse<List<ExerciseSubmissionHistoryResponse>>> GetExerciseSubmissionsHistoryAsync(Guid userId, Guid exerciseId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return PagedResponse<List<ExerciseSubmissionHistoryResponse>>.Fail(new object(), "Access denied. Invalid authentication.", 401);

                var exercise = await _unitOfWork.Exercises.GetByIdAsync(exerciseId);
                if (exercise == null)
                    return PagedResponse<List<ExerciseSubmissionHistoryResponse>>.Fail(new object(), "Exercise not found", 404);

                var lesson = await _unitOfWork.Lessons.GetByIdAsync(exercise.LessonID);
                if (lesson == null)
                    return PagedResponse<List<ExerciseSubmissionHistoryResponse>>.Fail(new object(), "Lesson not found", 404);

                var unit = await _unitOfWork.CourseUnits.GetByIdAsync(lesson.CourseUnitID);
                if (unit == null)
                    return PagedResponse<List<ExerciseSubmissionHistoryResponse>>.Fail(new object(), "Unit not found", 404);

                var course = await _unitOfWork.Courses.GetByIdAsync(unit.CourseID);
                if (course == null)
                    return PagedResponse<List<ExerciseSubmissionHistoryResponse>>.Fail(new object(), "Course not found", 404);

                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == user.UserID && l.LanguageId == course.LanguageId);

                if (learner == null)
                    return PagedResponse<List<ExerciseSubmissionHistoryResponse>>.Fail(new List<ExerciseSubmissionHistoryResponse>(), "Learner not found", 403);

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var baseQuery = _unitOfWork.ExerciseSubmissions.Query()
                    .Where(es => es.LearnerId == learner.LearnerLanguageId && es.ExerciseId == exerciseId);

                var totalCount = await baseQuery.CountAsync();

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var submissions = await baseQuery
                    .OrderByDescending(es => es.SubmittedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var responses = submissions.Select(es => new ExerciseSubmissionHistoryResponse
                {
                    ExerciseSubmissionId = es.ExerciseSubmissionId,
                    SubmittedAt = es.SubmittedAt.ToString("dd-MM-yyyy HH:mm"),
                    Status = es.Status.ToString(),
                    FinalScore = es.FinalScore,
                    IsPassed = es.IsPassed,
                    AudioUrl = es.AudioUrl,
                    TeacherFeedback = es.TeacherFeedback,
                    AIFeedback = es.AIFeedback,
                    AIScore = es.AIScore,
                    TeacherScore = es.TeacherScore,
                }).ToList();

                return PagedResponse<List<ExerciseSubmissionHistoryResponse>>.Success(
                    responses,
                    pageNumber,
                    pageSize,
                    totalPages,
                    "Submission history retrieved successfully",
                    200);
            }
            catch (Exception ex)
            {
                return PagedResponse<List<ExerciseSubmissionHistoryResponse>>.Error($"Error retrieving submission history: {ex.Message}");
            }
        }
        public async Task<BaseResponse<ProgressTrackingResponse>> GetCurrentProgressAsync(Guid userId, Guid courseId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Access denied. Invalid authentication.", 401);

                var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
                if (course == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Course not found", 404);

                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == user.UserID && l.LanguageId == course.LanguageId);
                if (learner == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Learner not found", 404);

                var enrollment = await _unitOfWork.Enrollments.FindAsync(e => e.LearnerId == learner.LearnerLanguageId && e.CourseId == courseId);

                if (enrollment == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Enrollment not found", 404);

                var currentUnitProgress = await _unitOfWork.UnitProgresses
                    .Query()
                    .FirstOrDefaultAsync(up => up.EnrollmentId == enrollment.EnrollmentID &&
                                             up.CourseUnitId == enrollment.CurrentUnitId);

                var currentLessonProgress = await _unitOfWork.LessonProgresses
                    .Query()
                    .FirstOrDefaultAsync(lp => currentUnitProgress != null && lp.UnitProgressId == currentUnitProgress.UnitProgressId &&
                                             lp.LessonId == enrollment.CurrentLessonId);

                if (currentUnitProgress == null || currentLessonProgress == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "No active progress found", 404);

                return await BuildProgressResponse(enrollment.EnrollmentID,
                    currentUnitProgress.UnitProgressId, currentLessonProgress.LessonProgressId);
            }
            catch (Exception ex)
            {
                return BaseResponse<ProgressTrackingResponse>.Error($"Error getting progress: {ex.Message}");
            }
        }
        public async Task<BaseResponse<ProgressTrackingResponse>> StartLessonAsync(Guid userId, StartLessonRequest request)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Access denied. Invalid authentication.", 401);

                var lesson = await _unitOfWork.Lessons.GetByIdAsync(request.LessonId);
                if (lesson == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Lesson not found", 404);

                if (lesson.CourseUnitID != request.UnitId)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Lesson does not belong to the specified unit", 404);

                var unit = await _unitOfWork.CourseUnits.GetByIdAsync(request.UnitId);
                if (unit == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Unit not found", 404);

                var course = await _unitOfWork.Courses.GetByIdAsync(unit.CourseID);
                if (course == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Course not found", 404);

                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == user.UserID && l.LanguageId == course.LanguageId);
                if (learner == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Access denied", 403);

                var enrollment = await _unitOfWork.Enrollments.Query()
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.LearnerId == learner.LearnerLanguageId &&
                                            e.Course.CourseUnits.Any(u => u.CourseUnitID == request.UnitId));

                if (enrollment == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Enrollment not found", 404);

                var unitProgress = await _unitOfWork.UnitProgresses.Query()
                    .FirstOrDefaultAsync(up => up.EnrollmentId == enrollment.EnrollmentID &&
                                             up.CourseUnitId == request.UnitId);

                if (unitProgress == null)
                {
                    unitProgress = new UnitProgress
                    {
                        UnitProgressId = Guid.NewGuid(),
                        EnrollmentId = enrollment.EnrollmentID,
                        CourseUnitId = request.UnitId,
                        Status = LearningStatus.InProgress,
                        StartedAt = TimeHelper.GetVietnamTime(),
                        LastUpdated = TimeHelper.GetVietnamTime()
                    };
                    await _unitOfWork.UnitProgresses.CreateAsync(unitProgress);
                }

                var lessonProgress = await _unitOfWork.LessonProgresses
                    .Query()
                    .FirstOrDefaultAsync(lp => lp.UnitProgressId == unitProgress.UnitProgressId &&
                                             lp.LessonId == request.LessonId);

                if (lessonProgress == null)
                {
                    lessonProgress = new LessonProgress
                    {
                        LessonProgressId = Guid.NewGuid(),
                        UnitProgressId = unitProgress.UnitProgressId,
                        LessonId = request.LessonId,
                        Status = LearningStatus.InProgress,
                        StartedAt = TimeHelper.GetVietnamTime(),
                        LastUpdated = TimeHelper.GetVietnamTime()
                    };
                    await _unitOfWork.LessonProgresses.CreateAsync(lessonProgress);

                    // Award small XP for starting a lesson (once)
                    await _gamificationService.AwardXpAsync(learner, 5, "Start lesson");
                }

                enrollment.CurrentUnitId = request.UnitId;
                enrollment.CurrentLessonId = request.LessonId;
                enrollment.LastAccessedAt = TimeHelper.GetVietnamTime();
                await _unitOfWork.Enrollments.UpdateAsync(enrollment);

                await _unitOfWork.SaveChangesAsync();
                return await BuildProgressResponse(enrollment.EnrollmentID, unitProgress.UnitProgressId, lessonProgress.LessonProgressId);
            });
        }
        public async Task<BaseResponse<ExerciseSubmissionResponse>> SubmitExerciseAsync(Guid userId, SubmitExerciseRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.FindAsync(u => u.UserID == userId);
                if (user == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Access denied. Invalid authentication.", 401);

                var exercise = await _unitOfWork.Exercises.GetByIdAsync(request.ExerciseId);
                if (exercise == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Exercise not found", 404);

                var lesson = await _unitOfWork.Lessons.GetByIdAsync(exercise.LessonID);
                if (lesson == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Lesson not found", 404);

                var unit = await _unitOfWork.CourseUnits.GetByIdAsync(lesson.CourseUnitID);
                if (unit == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Unit not found", 404);

                var course = await _unitOfWork.Courses.Query()
                    .Include(c => c.Language).Where(c => c.CourseID == unit.CourseID).FirstOrDefaultAsync();

                if (course == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Course not found", 404);

                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == user.UserID && l.LanguageId == course.LanguageId);
                if (learner == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Access denied", 403);

                var enrollment = await _unitOfWork.Enrollments.FindAsync(e => e.LearnerId == learner.LearnerLanguageId && e.CourseId == course.CourseID);
                if (enrollment == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Enrollment not found", 404);

                var unitProgress = await _unitOfWork.UnitProgresses.FindAsync(up => up.EnrollmentId == enrollment.EnrollmentID);
                if (unitProgress == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Unit progress not found", 404);

                var lessonProgress = await _unitOfWork.LessonProgresses.FindAsync(lp => lp.UnitProgressId == unitProgress.UnitProgressId);
                if (lessonProgress == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Lesson progress not found", 404);

                string audioUrl = string.Empty;
                string publicId = string.Empty;
                try
                {
                    var uploadResult = await _cloudinaryService.UploadAudioAsync(request.Audio);
                    if (uploadResult == null)
                        return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Upload audio failed", 404);
                    audioUrl = uploadResult.Url;
                    publicId = uploadResult.PublicId;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return BaseResponse<ExerciseSubmissionResponse>.Error($"Exception: {ex.Message}", 500, new object());
                }

                bool isAITeacherGrading = course.GradingType == GradingType.AIAndTeacher;
                bool isAIOnlyGrading = course.GradingType == GradingType.AIOnly;

                double aiPercentage = 100;
                double teacherPercentage = 0;
                decimal exerciseGradingAmount = 0;
                bool canAssignToTeacher = false;

                if (isAITeacherGrading)
                {
                    var purchase = await _unitOfWork.Purchases.Query()
                        .FirstOrDefaultAsync(p => p.UserId == userId &&
                                                p.CourseId == course.CourseID &&
                                                p.Status == PurchaseStatus.Completed);

                    if (purchase != null)
                    {
                        var totalExercisesInCourse = await _unitOfWork.Exercises.Query()
                            .CountAsync(e => e.Lesson != null && e.Lesson.CourseUnit != null && e.Lesson.CourseUnit.CourseID == course.CourseID);

                        if (totalExercisesInCourse > 0)
                        {
                            decimal amountForDistribution = purchase.FinalAmount * 0.9m;
                            decimal courseFeeAmount = amountForDistribution * 0.55m;
                            decimal gradingFeeAmount = amountForDistribution * 0.35m;
                            exerciseGradingAmount = gradingFeeAmount / totalExercisesInCourse;
                            var existingTeacherSubmissions = await _unitOfWork.ExerciseSubmissions.Query()
                                .CountAsync(es => es.LearnerId == learner.LearnerLanguageId &&
                                                es.ExerciseId == request.ExerciseId &&
                                                es.TeacherScore > 0);
                            canAssignToTeacher = existingTeacherSubmissions == 0;
                        }
                    }

                    if (canAssignToTeacher)
                    {
                        aiPercentage = DefaultAIPercentage;
                        teacherPercentage = DefaultTeacherPercentage;
                    }
                }

                bool isTeacherRequired = isAITeacherGrading &&
                                       canAssignToTeacher &&
                                       (exercise.Type == SpeakingExerciseType.StoryTelling ||
                                        exercise.Type == SpeakingExerciseType.Debate);

                if (exercise.Type == SpeakingExerciseType.RepeatAfterMe ||
                    exercise.Type == SpeakingExerciseType.PictureDescription)
                {
                    aiPercentage = 100;
                    teacherPercentage = 0;
                    isTeacherRequired = false;
                }

                var submission = new ExerciseSubmission
                {
                    ExerciseSubmissionId = Guid.NewGuid(),
                    LearnerId = learner.LearnerLanguageId,
                    ExerciseId = request.ExerciseId,
                    LessonProgressId = lessonProgress.LessonProgressId,
                    AudioUrl = audioUrl,
                    AudioPublicId = publicId,
                    AIPercentage = aiPercentage,
                    TeacherPercentage = teacherPercentage,
                    AIFeedback = "Pending AI Evaluation",
                    TeacherFeedback = (exercise.Type is SpeakingExerciseType.StoryTelling or SpeakingExerciseType.Debate) ? "Pending Teacher Evaluation" : string.Empty,
                    Status = isTeacherRequired ? ExerciseSubmissionStatus.PendingTeacherReview : ExerciseSubmissionStatus.PendingAIReview,
                    SubmittedAt = TimeHelper.GetVietnamTime(),
                };

                await _unitOfWork.ExerciseSubmissions.CreateAsync(submission);

                if (isTeacherRequired && course?.TeacherId != null)
                {
                    var gradingAssignment = new ExerciseGradingAssignment
                    {
                        GradingAssignmentId = Guid.NewGuid(),
                        ExerciseSubmissionId = submission.ExerciseSubmissionId,
                        AssignedTeacherId = course.TeacherId,
                        AssignedAt = TimeHelper.GetVietnamTime(),
                        Status = GradingStatus.Assigned,
                        DeadlineAt = TimeHelper.GetVietnamTime().AddDays(2),
                        CreatedAt = TimeHelper.GetVietnamTime()
                    };

                    await _unitOfWork.ExerciseGradingAssignments.CreateAsync(gradingAssignment);

                    if (exerciseGradingAmount > 0)
                    {
                        var earningAllocation = new TeacherEarningAllocation
                        {
                            AllocationId = Guid.NewGuid(),
                            TeacherId = course.TeacherId,
                            GradingAssignmentId = gradingAssignment.GradingAssignmentId,
                            ExerciseGradingAmount = exerciseGradingAmount,
                            EarningType = EarningType.ExerciseGrading,
                            Status = EarningStatus.Pending,
                            CreatedAt = TimeHelper.GetVietnamTime()
                        };
                        await _unitOfWork.TeacherEarningAllocations.CreateAsync(earningAllocation);
                    }
                }

                // XP: submitting an exercise earns small XP
                await _gamificationService.AwardXpAsync(learner, 10, "Submit exercise");

                lessonProgress.LastUpdated = TimeHelper.GetVietnamTime();
                await _unitOfWork.LessonProgresses.UpdateAsync(lessonProgress);

                await _unitOfWork.SaveChangesAsync();

                AssessmentRequest assessmentRequest = new AssessmentRequest
                {
                    ExerciseSubmissionId = submission.ExerciseSubmissionId,
                    AudioUrl = audioUrl,
                    LanguageCode = (course != null) ? course.Language.LanguageCode : "en",
                    GradingType = isTeacherRequired ? GradingType.AIAndTeacher.ToString() : GradingType.AIOnly.ToString()
                };

                await _exerciseGradingService.ProcessAIGradingAsync(assessmentRequest);

                var response = new ExerciseSubmissionResponse
                {
                    ExerciseSubmissionId = submission.ExerciseSubmissionId,
                    ExerciseId = submission.ExerciseId,
                    AIScore = submission.AIScore,
                    AIFeedback = submission.AIFeedback,
                    TeacherScore = submission.TeacherScore,
                    TeacherFeedback = submission.TeacherFeedback,
                    FinalScore = submission.FinalScore,
                    IsPassed = submission.IsPassed,
                    Status = submission.Status.ToString(),
                    SubmittedAt = submission.SubmittedAt.ToString("dd-MM-yyyy HH:mm")
                };

                return BaseResponse<ExerciseSubmissionResponse>.Success(response, "Exercise submitted successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<ExerciseSubmissionResponse>.Error($"Error: {ex.Message}", 500, new object());
                throw new Exception(ex.Message);
            }
        }
        public async Task<BaseResponse<ProgressTrackingResponse>> TrackActivityAsync(Guid userId, TrackActivityRequest request)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Access denied. Invalid authentication.", 401);

                var lesson = await _unitOfWork.Lessons.GetByIdAsync(request.LessonId);
                if (lesson == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Lesson not found", 404);

                var unit = await _unitOfWork.CourseUnits.GetByIdAsync(lesson.CourseUnitID);
                if (unit == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Unit not found", 404);

                var course = await _unitOfWork.Courses.GetByIdAsync(unit.CourseID);
                if (course == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Course not found", 404);

                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == user.UserID && l.LanguageId == course.LanguageId);
                if (learner == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Learner not found", 404);

                var enrollment = await _unitOfWork.Enrollments.FindAsync(e => e.LearnerId == learner.LearnerLanguageId && e.CourseId == course.CourseID);
                if (enrollment == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Enrollment not found", 404);

                var unitProgress = await _unitOfWork.UnitProgresses.FindAsync(up => up.EnrollmentId == enrollment.EnrollmentID);
                if (unitProgress == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Unit progress not found", 404);

                var lessonProgress = await _unitOfWork.LessonProgresses.FindAsync(lp => lp.UnitProgressId == unitProgress.UnitProgressId);

                if (lessonProgress == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Lesson progress not found", 404);

                var activityLog = new LessonActivityLog
                {
                    LessonActivityLogId = Guid.NewGuid(),
                    LessonId = request.LessonId,
                    LessonProgressId = lessonProgress.LessonProgressId,
                    LearnerId = learner.LearnerLanguageId,
                    ActivityType = Enum.Parse<LessonLogType>(request.LogType.ToString()),
                    Value = request.DurationMinutes,
                    MetadataJson = request.Metadata,
                    CreatedAt = TimeHelper.GetVietnamTime()
                };
                await _unitOfWork.LessonActivityLogs.CreateAsync(activityLog);

                int xp = request.LogType switch
                {
                    LessonLogType.ContentRead => 5,
                    LessonLogType.VideoProgress => request.DurationMinutes.HasValue ? (int)Math.Min(10, Math.Max(1, request.DurationMinutes.Value / 5)) : 3,
                    LessonLogType.PdfOpened => 2,
                    LessonLogType.ExercisePassed => 15,
                    LessonLogType.ExerciseFailed => 0,
                    _ => 1
                };
                if (xp > 0)
                {
                    await _gamificationService.AwardXpAsync(learner, xp, $"Activity {request.LogType}");
                }

                await UpdateLessonProgress(lessonProgress, request.LogType, request.DurationMinutes ?? 0);

                await _unitOfWork.SaveChangesAsync();

                return await BuildProgressResponse(enrollment.EnrollmentID, unitProgress.UnitProgressId, lessonProgress.LessonProgressId);
            });
        }
        #region
        private ExerciseSubmissionDetailResponse MapToDetailResponse(ExerciseSubmission submission)
        {
            var exercise = submission.Exercise;
            var lesson = exercise.Lesson;
            var unit = lesson.CourseUnit;
            var course = unit.Course;

            var teacherAssignment = submission.ExerciseGradingAssignments
                .FirstOrDefault(ega => ega.Status == GradingStatus.Returned);


            return new ExerciseSubmissionDetailResponse
            {
                ExerciseSubmissionId = submission.ExerciseSubmissionId,
                ExerciseId = exercise.ExerciseID,
                ExerciseTitle = exercise.Title,
                ExerciseDescription = exercise.Content,
                ExerciseType = exercise.Type.ToString(),
                PassScore = exercise.PassScore,
                AudioUrl = submission.AudioUrl,
                SubmittedAt = submission.SubmittedAt.ToString("dd-MM-yyyy HH:mm"),
                Status = submission.Status.ToString(),
                AIScore = submission.AIScore,
                AIFeedback = submission.AIFeedback,
                TeacherScore = submission.TeacherScore,
                TeacherFeedback = submission.TeacherFeedback,
                FinalScore = (submission.TeacherScore == 0) ? submission.AIScore : (submission.AIScore + submission.TeacherScore) / 2,
                IsPassed = submission.IsPassed,
                ReviewedAt = submission.ReviewedAt?.ToString("dd-MM-yyyy HH:mm"),
                LessonId = lesson.LessonID,
                LessonTitle = lesson.Title,
                UnitId = unit.CourseUnitID,
                UnitTitle = unit.Title,
                CourseId = course.CourseID,
                CourseTitle = course.Title,
                TeacherId = teacherAssignment?.AssignedTeacherId,
                TeacherName = teacherAssignment?.Teacher?.User?.FullName,
                TeacherAvatar = teacherAssignment?.Teacher?.Avatar,
            };
        }
        private async Task UpdateLessonProgress(LessonProgress lessonProgress, LessonLogType lessonLogType, double durationMinutes)
        {
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonProgress.LessonId);
            if (lesson == null) return;

            // Update activity flags
            switch (lessonLogType)
            {
                case LessonLogType.ContentRead:
                    lessonProgress.IsContentViewed = true;
                    break;
                case LessonLogType.VideoProgress:
                    lessonProgress.IsVideoWatched = true;
                    break;
                case LessonLogType.PdfOpened:
                    lessonProgress.IsDocumentRead = true;
                    break;
            }

            // Calculate progress percent
            double progress = 0.0;
            int completedActivities = 0;
            int totalActivities = 1; // Content is always required

            // Content is mandatory (50%)
            if (lessonProgress.IsContentViewed == true)
            {
                progress += 0.5;
                completedActivities++;
            }

            // Video is optional (20% if exists)
            if (lesson.VideoUrl != null)
            {
                totalActivities++;
                if (lessonProgress.IsVideoWatched == true)
                {
                    progress += 0.2;
                    completedActivities++;
                }
            }

            // Document is optional (20% if exists)
            if (lesson.DocumentUrl != null)
            {
                totalActivities++;
                if (lessonProgress.IsDocumentRead == true)
                {
                    progress += 0.2;
                    completedActivities++;
                }
            }

            var exercises = await _unitOfWork.Exercises
                .Query()
                .Where(e => e.LessonID == lessonProgress.LessonId)
                .ToListAsync();

            if (exercises.Any())
            {
                totalActivities++;
                var completedExercises = await _unitOfWork.ExerciseSubmissions
                    .Query()
                    .Where(es => es.LessonProgressId == lessonProgress.LessonProgressId &&
                                es.Status == ExerciseSubmissionStatus.Passed)
                    .CountAsync();

                if (completedExercises == exercises.Count)
                {
                    progress += 0.3;
                    completedActivities++;
                    lessonProgress.IsPracticeCompleted = true;
                }
                else if (completedExercises > 0)
                {
                    progress += (0.3 * completedExercises / exercises.Count);
                }
            }

            lessonProgress.ProgressPercent = Math.Min(progress * 100, 100);
            lessonProgress.LastUpdated = TimeHelper.GetVietnamTime();

            // Update status
            if (lessonProgress.ProgressPercent >= 100)
            {
                lessonProgress.Status = LearningStatus.Completed;
                lessonProgress.CompletedAt = TimeHelper.GetVietnamTime();

                // Update unit progress
                await UpdateUnitProgress(lessonProgress.UnitProgressId);

                // XP: completing a lesson grants bonus XP
                var learner = await _unitOfWork.LearnerLanguages.GetByIdAsync(lessonProgress.UnitProgress.Enrollment.LearnerId);
                if (learner != null)
                {
                    await _gamificationService.AwardXpAsync(learner, 25, "Complete lesson");
                }
            }
            else if (lessonProgress.ProgressPercent > 0)
            {
                lessonProgress.Status = LearningStatus.InProgress;
            }

            await _unitOfWork.LessonProgresses.UpdateAsync(lessonProgress);
        }
        private async Task UpdateUnitProgress(Guid unitProgressId)
        {
            var unitProgress = await _unitOfWork.UnitProgresses
                .Query()
                .Include(up => up.LessonProgresses)
                .Include(up => up.Enrollment)
                .FirstOrDefaultAsync(up => up.UnitProgressId == unitProgressId);

            if (unitProgress == null) return;

            var unit = await _unitOfWork.CourseUnits.GetByIdAsync(unitProgress.CourseUnitId);
            if (unit == null) return;

            var lessonsInUnit = await _unitOfWork.Lessons
                .Query()
                .Where(l => l.CourseUnitID == unitProgress.CourseUnitId)
                .CountAsync();

            var completedLessons = unitProgress.LessonProgresses
                .Count(lp => lp.Status == LearningStatus.Completed);

            unitProgress.ProgressPercent = lessonsInUnit > 0 ? (completedLessons * 100.0 / lessonsInUnit) : 0;
            unitProgress.LastUpdated = TimeHelper.GetVietnamTime();

            if (unitProgress.ProgressPercent >= 100)
            {
                unitProgress.Status = LearningStatus.Completed;
                unitProgress.CompletedAt = TimeHelper.GetVietnamTime();

                // Update enrollment progress
                await UpdateEnrollmentProgress(unitProgress.EnrollmentId);

                // XP: completing a unit grants higher bonus XP
                var learner = await _unitOfWork.LearnerLanguages.GetByIdAsync(unitProgress.Enrollment.LearnerId);
                if (learner != null)
                {
                    await _gamificationService.AwardXpAsync(learner, 60, "Complete unit");
                }
            }
            else if (unitProgress.ProgressPercent > 0)
            {
                unitProgress.Status = LearningStatus.InProgress;
            }

            await _unitOfWork.UnitProgresses.UpdateAsync(unitProgress);
        }
        private async Task UpdateEnrollmentProgress(Guid enrollmentId)
        {
            var enrollment = await _unitOfWork.Enrollments
                .Query()
                .Include(e => e.Course)
                .ThenInclude(c => c.CourseUnits)
                .Include(e => e.UnitProgresses)
                .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);

            if (enrollment == null) return;

            var totalUnits = enrollment.Course.CourseUnits.Count;
            var completedUnits = enrollment.UnitProgresses
                .Count(up => up.Status == LearningStatus.Completed);

            var totalLessons = await _unitOfWork.Lessons
                .Query()
                .CountAsync(l => enrollment.Course.CourseUnits.Select(u => u.CourseUnitID).Contains(l.CourseUnitID));

            var completedLessons = await _unitOfWork.LessonProgresses
                .Query()
                .CountAsync(lp => lp.UnitProgress.EnrollmentId == enrollmentId &&
                                lp.Status == LearningStatus.Completed);

            enrollment.CompletedUnits = completedUnits;
            enrollment.TotalUnits = totalUnits;
            enrollment.CompletedLessons = completedLessons;
            enrollment.TotalLessons = totalLessons;
            enrollment.ProgressPercent = totalUnits > 0 ? (completedUnits * 100.0 / totalUnits) : 0;
            enrollment.LastAccessedAt = TimeHelper.GetVietnamTime();

            if (enrollment.ProgressPercent >= 100)
            {
                enrollment.Status = DAL.Type.EnrollmentStatus.Completed;
                enrollment.CompletedAt = TimeHelper.GetVietnamTime();

                // XP: completing a course grants big bonus XP
                var learner = await _unitOfWork.LearnerLanguages.GetByIdAsync(enrollment.LearnerId);
                if (learner != null)
                {
                    await _gamificationService.AwardXpAsync(learner, 200, "Complete course");
                }
            }

            await _unitOfWork.Enrollments.UpdateAsync(enrollment);
        }
        private async Task<BaseResponse<ProgressTrackingResponse>> BuildProgressResponse(Guid enrollmentId, Guid unitProgressId, Guid lessonProgressId)
        {
            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId);
            var unitProgress = await _unitOfWork.UnitProgresses.GetByIdAsync(unitProgressId);
            var lessonProgress = await _unitOfWork.LessonProgresses.GetByIdAsync(lessonProgressId);

            if (enrollment == null || unitProgress == null || lessonProgress == null)
                return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Progress data not found", 404);

            var response = new ProgressTrackingResponse
            {
                EnrollmentId = enrollmentId,
                UnitProgressId = unitProgressId,
                LessonProgressId = lessonProgressId,
                LessonProgressPercent = lessonProgress.ProgressPercent,
                UnitProgressPercent = unitProgress.ProgressPercent,
                EnrollmentProgressPercent = enrollment.ProgressPercent,
                LessonStatus = lessonProgress.Status.ToString(),
                UnitStatus = unitProgress.Status.ToString(),
                TotalTimeSpentMinutes = enrollment.TotalTimeSpent,
                LastAccessedAt = enrollment.LastAccessedAt?.ToString("dd-MM-yyyy")
            };

            return BaseResponse<ProgressTrackingResponse>.Success(response, "Progress retrieved successfully");
        }
        #endregion
    }
}
