using BLL.IServices.Assessment;
using BLL.IServices.ProgressTracking;
using BLL.IServices.Wallets;
using BLL.Services.Wallets;
using Common.DTO.ApiResponse;
using Common.DTO.ExerciseGrading.Request;
using Common.DTO.ExerciseGrading.Response;
using Common.DTO.Paging.Response;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.ProgressTracking
{
    public class ExerciseGradingService : IExerciseGradingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAssessmentService _assessmentService;
        private readonly IWalletService _walletService;
        public ExerciseGradingService(IUnitOfWork unitOfWork, IAssessmentService assessmentService, WalletService walletService)
        {
            _unitOfWork = unitOfWork;
            _assessmentService = assessmentService;
            _walletService = walletService;
        }
        public async Task<BaseResponse<bool>> CheckAndReassignExpiredAssignmentsAsync()
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                try
                {
                    var now = TimeHelper.GetVietnamTime();
                    var batchSize = 100;
                    var totalProcessed = 0;

                    while (true)
                    {
                        var expiredAssignments = await _unitOfWork.ExerciseGradingAssignments
                            .Query()
                            .Include(a => a.ExerciseSubmission)
                            .Include(a => a.EarningAllocation)
                            .Where(a => a.Status == GradingStatus.Assigned &&
                                       a.DeadlineAt < now)
                            .OrderBy(a => a.DeadlineAt)
                            .Take(batchSize)
                            .ToListAsync();

                        if (!expiredAssignments.Any())
                            break;

                        foreach (var assignment in expiredAssignments)
                        {
                            if (assignment.EarningAllocation != null)
                            {
                                assignment.EarningAllocation.Status = EarningStatus.Rejected;
                                assignment.EarningAllocation.UpdatedAt = now;
                                await _unitOfWork.TeacherEarningAllocations.UpdateAsync(assignment.EarningAllocation);
                            }

                            assignment.Status = GradingStatus.Expired;
                            assignment.CompletedAt = now;
                            await _unitOfWork.ExerciseGradingAssignments.UpdateAsync(assignment);

                            var submission = assignment.ExerciseSubmission;
                            submission.Status = ExerciseSubmissionStatus.PendingTeacherReview;
                            submission.TeacherFeedback = "Giáo viên đang chấm bài của bạn. Vui lòng chờ để hệ thống cập nhật kết quả khi quá trình hoàn tất.";
                            await _unitOfWork.ExerciseSubmissions.UpdateAsync(submission);
                        }

                        await _unitOfWork.SaveChangesAsync();
                        totalProcessed += expiredAssignments.Count;

                        if (expiredAssignments.Count < batchSize)
                            break;
                    }

                    return BaseResponse<bool>.Success(true,
                        totalProcessed > 0
                            ? $"Successfully expired {totalProcessed} overdue assignments"
                            : "No expired assignments found");
                }
                catch (Exception ex)
                {
                    return BaseResponse<bool>.Error($"Error expiring assignments: {ex.Message}");
                }
            });
        }
        public async Task<BaseResponse<bool>> AssignExerciseToTeacherAsync(Guid exerciseSubmissionId, Guid userId, Guid teacherId)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                try
                {
                    var manager = await _unitOfWork.ManagerLanguages.FindAsync(m => m.UserId == userId);
                    if (manager == null)
                        return BaseResponse<bool>.Fail(false, "Access denied. Manager privileges required.", 403);

                    var submission = await _unitOfWork.ExerciseSubmissions
                        .Query()
                        .Include(es => es.Exercise)
                            .ThenInclude(e => e.Lesson)
                                .ThenInclude(l => l.CourseUnit)
                                    .ThenInclude(cu => cu.Course)
                        .Include(es => es.ExerciseGradingAssignments)
                            .ThenInclude(ega => ega.EarningAllocation)
                        .FirstOrDefaultAsync(es => es.ExerciseSubmissionId == exerciseSubmissionId);

                    if (submission == null)
                        return BaseResponse<bool>.Fail(false, "Exercise submission not found", 404);

                    var now = TimeHelper.GetVietnamTime();
                    var hasOverdueAssignment = submission.ExerciseGradingAssignments
                        .Any(a => a.Status == GradingStatus.Expired ||
                                 (a.Status == GradingStatus.Assigned && a.DeadlineAt < now));

                    if (!hasOverdueAssignment)
                        return BaseResponse<bool>.Fail(false, "Can only reassign overdue or expired assignments", 400);

                    var teacher = await _unitOfWork.TeacherProfiles.Query()
                        .Include(t => t.User)
                        .FirstOrDefaultAsync(t => t.TeacherId == teacherId);

                    if (teacher == null)
                        return BaseResponse<bool>.Fail(false, "Teacher not found", 404);

                    var courseLanguage = submission.Exercise.Lesson.CourseUnit.Course.LanguageId;

                    if (teacher.LanguageId != courseLanguage)
                        return BaseResponse<bool>.Fail(false, "Teacher language does not match exercise language", 400);

                    var oldAssignment = submission.ExerciseGradingAssignments
                        .FirstOrDefault(a => a.Status == GradingStatus.Expired ||
                                           (a.Status == GradingStatus.Assigned && a.DeadlineAt < now));

                    if (oldAssignment == null)
                        return BaseResponse<bool>.Fail(false, "No expired or overdue assignment found", 400);

                    var activeAssignments = submission.ExerciseGradingAssignments
                        .Where(a => a.Status == GradingStatus.Assigned)
                        .ToList();

                    foreach (var activeAssignment in activeAssignments)
                    {
                        if (activeAssignment.EarningAllocation != null)
                        {
                            activeAssignment.EarningAllocation.Status = EarningStatus.Rejected;
                            activeAssignment.EarningAllocation.UpdatedAt = now;
                            await _unitOfWork.TeacherEarningAllocations.UpdateAsync(activeAssignment.EarningAllocation);
                        }

                        activeAssignment.Status = GradingStatus.Cancelled;
                        activeAssignment.CompletedAt = now;
                        await _unitOfWork.ExerciseGradingAssignments.UpdateAsync(activeAssignment);
                    }

                    var deadline = now.AddHours(48);

                    var newAssignment = new ExerciseGradingAssignment
                    {
                        GradingAssignmentId = Guid.NewGuid(),
                        ExerciseSubmissionId = exerciseSubmissionId,
                        AssignedTeacherId = teacherId,
                        Status = GradingStatus.Assigned,
                        AssignedAt = now,
                        DeadlineAt = deadline,
                        CreatedAt = now,
                    };

                    await _unitOfWork.ExerciseGradingAssignments.AddAsync(newAssignment);

                    decimal exerciseGradingAmount = 0;

                    var course = submission.Exercise.Lesson.CourseUnit.Course;
                    var learner = submission.Learner;

                    var purchase = await _unitOfWork.Purchases.Query()
                        .FirstOrDefaultAsync(p => p.UserId == learner.UserId &&
                                                p.CourseId == course.CourseID &&
                                                p.Status == PurchaseStatus.Completed);

                    if (purchase != null)
                    {
                        var teacherGradingExercises = await _unitOfWork.Exercises.Query()
                            .CountAsync(e => e.Lesson != null &&
                                           e.Lesson.CourseUnit != null &&
                                           e.Lesson.CourseUnit.CourseID == course.CourseID &&
                                           (e.Type == SpeakingExerciseType.StoryTelling ||
                                            e.Type == SpeakingExerciseType.Debate));

                        if (teacherGradingExercises > 0)
                        {
                            decimal amountForDistribution = purchase.FinalAmount * 0.9m;
                            decimal gradingFeeAmount = amountForDistribution * 0.35m;
                            exerciseGradingAmount = gradingFeeAmount / teacherGradingExercises;
                        }
                    }

                    if (exerciseGradingAmount == 0 && oldAssignment.EarningAllocation != null)
                    {
                        exerciseGradingAmount = (decimal)oldAssignment.EarningAllocation.ExerciseGradingAmount;
                    }

                    var newAllocation = new TeacherEarningAllocation
                    {
                        AllocationId = Guid.NewGuid(),
                        GradingAssignmentId = newAssignment.GradingAssignmentId,
                        TeacherId = teacherId,
                        ExerciseGradingAmount = exerciseGradingAmount,
                        Status = EarningStatus.Pending,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    await _unitOfWork.TeacherEarningAllocations.AddAsync(newAllocation);
                    submission.Status = ExerciseSubmissionStatus.PendingTeacherReview;
                    await _unitOfWork.ExerciseSubmissions.UpdateAsync(submission);

                    await _unitOfWork.SaveChangesAsync();

                    return BaseResponse<bool>.Success(true, "Exercise successfully reassigned to teacher");
                }
                catch (Exception ex)
                {
                    return BaseResponse<bool>.Error($"Error assigning exercise to teacher: {ex.Message}");
                }
            });
        }
        public async Task<BaseResponse<ExerciseGradingStatusResponse>> GetGradingStatusAsync(Guid exerciseSubmissionId)
        {
            try
            {
                var submission = await _unitOfWork.ExerciseSubmissions
                    .Query()
                    .Include(es => es.Exercise)
                    .Include(es => es.ExerciseGradingAssignments)
                    .ThenInclude(a => a.Teacher)
                    .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(es => es.ExerciseSubmissionId == exerciseSubmissionId);

                if (submission == null)
                    return BaseResponse<ExerciseGradingStatusResponse>.Fail(new object(), "Submission not found", 404);

                var currentAssignment = submission.ExerciseGradingAssignments
                    .FirstOrDefault(a => a.Status == GradingStatus.Assigned);

                var response = new ExerciseGradingStatusResponse
                {
                    ExerciseSubmissionId = submission.ExerciseSubmissionId,
                    Status = submission.Status.ToString(),
                    AIScore = submission.AIScore,
                    TeacherScore = submission.TeacherScore,
                    FinalScore = submission.FinalScore,
                    IsPassed = submission.IsPassed,
                    AIFeedback = submission.AIFeedback,
                    TeacherFeedback = submission.TeacherFeedback,
                    SubmittedAt = submission.SubmittedAt.ToString("dd-MM-yyyy"),
                    ReviewedAt = submission.ReviewedAt?.ToString("dd-MM-yyyy"),
                    AssignedTeacherId = currentAssignment?.AssignedTeacherId,
                    AssignedTeacherName = currentAssignment?.Teacher?.User?.FullName,
                    AssignmentDeadline = currentAssignment?.DeadlineAt.ToString("dd-MM-yyyy")
                };

                return BaseResponse<ExerciseGradingStatusResponse>.Success(response, "Grading status retrieved");
            }
            catch (Exception ex)
            {
                return BaseResponse<ExerciseGradingStatusResponse>.Error($"Error getting grading status: {ex.Message}");
            }
        }
        public async Task<PagedResponse<List<ExerciseGradingAssignmentResponse>>> GetTeacherAssignmentsAsync(Guid userId, GradingAssignmentFilterRequest filter)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return PagedResponse<List<ExerciseGradingAssignmentResponse>>.Fail(new object(), "Access denied", 401);

                var teacher = await _unitOfWork.TeacherProfiles.FindAsync(t => t.UserId == userId);
                if (teacher == null)
                    return PagedResponse<List<ExerciseGradingAssignmentResponse>>.Fail(new object(), "Access denied", 403);

                var query = _unitOfWork.ExerciseGradingAssignments.Query()
                    .Include(a => a.ExerciseSubmission)
                        .ThenInclude(es => es.Exercise)
                            .ThenInclude(e => e.Lesson)
                                .ThenInclude(l => l.CourseUnit)
                                    .ThenInclude(cu => cu.Course)
                    .Include(a => a.ExerciseSubmission)
                        .ThenInclude(es => es.Learner)
                            .ThenInclude(l => l.User)
                    .Include(a => a.EarningAllocation)
                    .Where(a => a.AssignedTeacherId == teacher.TeacherId);

                if (!string.IsNullOrEmpty(filter.Status) &&
                    Enum.TryParse<GradingStatus>(filter.Status, out var statusFilter))
                {
                    query = query.Where(a => a.Status == statusFilter);
                }

                if (filter.ExerciseId.HasValue)
                {
                    query = query.Where(a => a.ExerciseSubmission.ExerciseId == filter.ExerciseId.Value);
                }

                if (filter.LessonId.HasValue)
                {
                    query = query.Where(a => a.ExerciseSubmission.Exercise.Lesson.LessonID == filter.LessonId.Value);
                }

                if (filter.CourseId.HasValue)
                {
                    query = query.Where(a => a.ExerciseSubmission.Exercise.Lesson.CourseUnit.CourseID == filter.CourseId.Value);
                }

                if (filter.FromDate.HasValue)
                {
                    query = query.Where(a => a.AssignedAt >= filter.FromDate.Value);
                }

                if (filter.ToDate.HasValue)
                {
                    query = query.Where(a => a.AssignedAt <= filter.ToDate.Value);
                }

                var totalCount = await query.CountAsync();

                var assignments = await query
                    .OrderByDescending(a => a.AssignedAt)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var now = TimeHelper.GetVietnamTime();
                var response = assignments.Select(a => new ExerciseGradingAssignmentResponse
                {
                    AssignmentId = a.GradingAssignmentId,
                    ExerciseSubmissionId = a.ExerciseSubmissionId,
                    LearnerId = a.ExerciseSubmission.LearnerId,
                    LearnerName = a.ExerciseSubmission.Learner.User?.FullName ?? "Unknown",
                    ExerciseId = a.ExerciseSubmission.ExerciseId,
                    ExerciseTitle = a.ExerciseSubmission.Exercise.Title,
                    ExerciseType = a.ExerciseSubmission.Exercise.Type.ToString(),
                    LessonId = a.ExerciseSubmission.Exercise.Lesson?.LessonID,
                    LessonTitle = a.ExerciseSubmission.Exercise.Lesson?.Title ?? "Unknown",
                    CourseId = a.ExerciseSubmission.Exercise.Lesson?.CourseUnit?.CourseID,
                    CourseName = a.ExerciseSubmission.Exercise.Lesson?.CourseUnit?.Course?.Title ?? "Unknown",
                    AudioUrl = a.ExerciseSubmission.AudioUrl,
                    AIScore = a.ExerciseSubmission.AIScore,
                    AIFeedback = a.ExerciseSubmission.AIFeedback,
                    Status = a.Status.ToString(),
                    GradingStatus = a.Status.ToString(),
                    EarningStatus = a.EarningAllocation?.Status.ToString() ?? "Not Allocated",
                    EarningAmount = a.EarningAllocation?.ExerciseGradingAmount ?? 0,
                    AssignedAt = a.AssignedAt.ToString("dd-MM-yyyy HH:mm"),
                    Deadline = a.DeadlineAt.ToString("dd-MM-yyyy HH:mm"),
                    StartedAt = a.StartedAt?.ToString("dd-MM-yyyy HH:mm"),
                    CompletedAt = a.CompletedAt?.ToString("dd-MM-yyyy HH:mm"),
                    IsOverdue = a.DeadlineAt < now && a.Status == GradingStatus.Assigned,
                    HoursRemaining = a.Status == GradingStatus.Assigned ?
                    Math.Max(0, (int)(a.DeadlineAt - now).TotalHours) : 0,
                    FinalScore = a.FinalScore,
                    Feedback = a.Feedback
                }).ToList();

                return PagedResponse<List<ExerciseGradingAssignmentResponse>>.Success(
                    response,
                    filter.Page,
                    filter.PageSize,
                    totalCount,
                    "Assignments retrieved successfully");
            }
            catch (Exception ex)
            {
                return PagedResponse<List<ExerciseGradingAssignmentResponse>>.Error($"Error getting assignments: {ex.Message}");
            }
        }
        [AutomaticRetry(Attempts = 5, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        public async Task<BaseResponse<bool>> ProcessAIGradingAsync(AssessmentRequest request)
        {
            var submission = await _unitOfWork.ExerciseSubmissions.Query()
                .Include(es => es.Exercise)
                .Include(es => es.Learner)
                    .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(es => es.ExerciseSubmissionId == request.ExerciseSubmissionId);

            if (submission == null)
                return BaseResponse<bool>.Fail(false, "Exercise submission not found", 404);

            if (submission.Status != ExerciseSubmissionStatus.PendingAIReview)
                return BaseResponse<bool>.Fail(false, "Submission is not pending AI review", 400);

            try
            {
                var exercise = submission.Exercise;
                var learner = submission.Learner;

                var aiEvaluation = await _assessmentService.EvaluateSpeakingAsync(request);

                submission.AIScore = aiEvaluation.Overall;
                submission.AIFeedback = aiEvaluation.ToString();
                submission.Status = ExerciseSubmissionStatus.AIGraded;

                bool isAutoPassExercise = exercise.Type == SpeakingExerciseType.RepeatAfterMe ||
                         exercise.Type == SpeakingExerciseType.PictureDescription;

                bool isTeacherRequiredExercise = exercise.Type == SpeakingExerciseType.StoryTelling ||
                                exercise.Type == SpeakingExerciseType.Debate;

                bool isAIOnlyGrading = request.GradingType == GradingType.AIOnly.ToString() ||
                      submission.TeacherPercentage == 0;

                bool isAITeacherGrading = request.GradingType == GradingType.AIAndTeacher.ToString() ||
                                         submission.TeacherPercentage > 0;

                double? finalScore = 0;

                if (isAIOnlyGrading)
                {
                    finalScore = submission.AIScore;
                }
                else if (isAITeacherGrading)
                {
                    if (submission.TeacherScore > 0)
                    {
                        finalScore = (submission.AIScore * submission.AIPercentage / 100) +
                                   (submission.TeacherScore * submission.TeacherPercentage / 100);
                    }
                    else
                    {
                        finalScore = submission.AIScore * submission.AIPercentage / 100;
                    }
                }

                submission.IsPassed = finalScore >= exercise.PassScore;
                submission.FinalScore = finalScore;

                if (isAutoPassExercise || isAIOnlyGrading)
                {
                    if (submission.IsPassed == true)
                    {
                        submission.Status = ExerciseSubmissionStatus.Passed;
                    }
                    else
                    {
                        submission.Status = ExerciseSubmissionStatus.Failed;
                    }

                    submission.ReviewedAt = TimeHelper.GetVietnamTime();
                    await _unitOfWork.SaveChangesAsync();

                    await UpdateLessonProgressAfterGrading(submission);
                }
                else if (isTeacherRequiredExercise && isAITeacherGrading)
                {
                    if (submission.TeacherScore > 0)
                    {
                        if ((bool)submission.IsPassed)
                        {
                            submission.Status = ExerciseSubmissionStatus.Passed;
                        }
                        else
                        {
                            submission.Status = ExerciseSubmissionStatus.Failed;
                        }

                        submission.ReviewedAt = TimeHelper.GetVietnamTime();
                        await _unitOfWork.SaveChangesAsync();

                        await UpdateLessonProgressAfterGrading(submission);
                    }
                    else
                    {
                        submission.Status = ExerciseSubmissionStatus.PendingTeacherReview;
                    }
                }
                else
                {
                    if ((bool)submission.IsPassed)
                    {
                        submission.Status = ExerciseSubmissionStatus.Passed;
                    }
                    else
                    {
                        submission.Status = ExerciseSubmissionStatus.Failed;
                    }

                    submission.ReviewedAt = TimeHelper.GetVietnamTime();
                    await _unitOfWork.SaveChangesAsync();

                    await UpdateLessonProgressAfterGrading(submission);
                }

                await _unitOfWork.ExerciseSubmissions.UpdateAsync(submission);
                await _unitOfWork.SaveChangesAsync();

                return BaseResponse<bool>.Success(true, "AI grading completed successfully");
            }
            catch (Exception ex)
            {
                submission.Status = ExerciseSubmissionStatus.Failed;
                submission.AIFeedback = $"AI grading failed: {ex.Message}";
                submission.AIScore = 0;
                await _unitOfWork.ExerciseSubmissions.UpdateAsync(submission);
                await _unitOfWork.SaveChangesAsync();
                return BaseResponse<bool>.Error($"AI grading failed: {ex.Message}");
            }
        }
        public async Task<BaseResponse<bool>> ProcessTeacherGradingAsync(Guid exerciseSubmissionId, Guid userId, double score, string feedback)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<bool>.Fail(new object(), "Access denied. Invalid authentication.", 401);

                if (score < 0 || score > 100)
                    return BaseResponse<bool>.Fail(false, "Score must be between 0 and 100", 400);

                var teacher = await _unitOfWork.TeacherProfiles.FindAsync(t => t.UserId == userId);
                if (teacher == null)
                    return BaseResponse<bool>.Fail(new object(), "Access denied", 403);

                var submission = await _unitOfWork.ExerciseSubmissions
                    .Query()
                    .Include(es => es.Exercise)
                    .Include(es => es.ExerciseGradingAssignments)
                        .ThenInclude(eg => eg.EarningAllocation)
                    .FirstOrDefaultAsync(es => es.ExerciseSubmissionId == exerciseSubmissionId);

                if (submission == null)
                    return BaseResponse<bool>.Fail(false, "Exercise submission not found", 404);

                var assignment = submission.ExerciseGradingAssignments
                    .FirstOrDefault(a => a.AssignedTeacherId == teacher.TeacherId &&
                                       a.Status == GradingStatus.Assigned);

                if (assignment == null)
                    return BaseResponse<bool>.Fail(false, "You are not assigned to grade this submission", 403);

                if (submission.Status != ExerciseSubmissionStatus.PendingTeacherReview &&
                    submission.Status != ExerciseSubmissionStatus.AIGraded)
                    return BaseResponse<bool>.Fail(false, "Submission is not ready for teacher grading", 400);

                var isResubmission = await IsResubmissionAfterTeacherFail(submission.LearnerId, submission.ExerciseId);
                var isFirstTeacherGrading = !isResubmission;

                submission.TeacherScore = score;
                submission.TeacherFeedback = feedback;

                double? finalScore = (submission.AIScore * submission.AIPercentage / 100) +
                   (submission.TeacherScore * (submission.TeacherPercentage ?? 0) / 100);

                submission.IsPassed = finalScore >= submission.Exercise.PassScore;

                if (submission.IsPassed == true)
                {
                    submission.Status = ExerciseSubmissionStatus.Passed;
                }
                else
                {
                    submission.Status = ExerciseSubmissionStatus.Failed;
                }

                submission.FinalScore = finalScore;
                submission.ReviewedAt = TimeHelper.GetVietnamTime();

                assignment.Status = GradingStatus.Returned;
                assignment.CompletedAt = TimeHelper.GetVietnamTime();
                assignment.FinalScore = score;
                assignment.Feedback = feedback;

                await _unitOfWork.ExerciseSubmissions.UpdateAsync(submission);
                await _unitOfWork.ExerciseGradingAssignments.UpdateAsync(assignment);

                var allocation = assignment.EarningAllocation;

                if (allocation != null && isFirstTeacherGrading)
                {
                    allocation.Status = EarningStatus.Approved;
                    allocation.ApprovedAt = TimeHelper.GetVietnamTime();
                    allocation.UpdatedAt = TimeHelper.GetVietnamTime();

                    await _unitOfWork.TeacherEarningAllocations.UpdateAsync(allocation);

                    BackgroundJob.Enqueue(() => _walletService.TransferExerciseGradingFeeToTeacherAsync(allocation.AllocationId));
                }

                await _unitOfWork.SaveChangesAsync();

                await UpdateLessonProgressAfterGrading(submission);

                return BaseResponse<bool>.Success(true, "Teacher grading completed successfully");
            });
        }
        #region
        private async Task UpdateLessonProgressAfterGrading(ExerciseSubmission submission)
        {
            var lessonProgress = await _unitOfWork.LessonProgresses
                .Query()
                .Include(lp => lp.ExerciseSubmissions)
                .Include(lp => lp.Lesson)
                    .ThenInclude(l => l.Exercises)
                .FirstOrDefaultAsync(lp => lp.LessonProgressId == submission.LessonProgressId);

            if (lessonProgress != null && lessonProgress.Lesson != null)
            {
                var wasExerciseAlreadyPassed = await _unitOfWork.ExerciseSubmissions
                    .Query()
                    .AnyAsync(es => es.ExerciseId == submission.ExerciseId &&
                                   es.LessonProgressId == submission.LessonProgressId &&
                                   es.IsPassed == true &&
                                   es.ExerciseSubmissionId != submission.ExerciseSubmissionId &&
                                   es.SubmittedAt < submission.SubmittedAt);

                Console.WriteLine($"[DEBUG]: Exercise {submission.ExerciseId} was already passed before: {wasExerciseAlreadyPassed}");

                if (wasExerciseAlreadyPassed && submission.IsPassed == true)
                {
                    Console.WriteLine($"[DEBUG]: Skipping progress update - exercise was already passed before");
                    return;
                }


                var passedExercises = lessonProgress.ExerciseSubmissions
                    .Where(es => es.IsPassed == true)
                    .Select(es => es.ExerciseId)
                    .Distinct()
                    .Count();

                var totalExercises = lessonProgress.Lesson.Exercises.Count;
                double exerciseProgressPercent = totalExercises > 0 ? (passedExercises * 100.0 / totalExercises) : 0;
                await UpdateOverallLessonProgress(lessonProgress, exerciseProgressPercent);

                lessonProgress.LastUpdated = TimeHelper.GetVietnamTime();

                await _unitOfWork.LessonProgresses.UpdateAsync(lessonProgress);
                await _unitOfWork.SaveChangesAsync();

                await UpdateUnitProgress(lessonProgress.UnitProgressId);
            }
        }
        private async Task UpdateOverallLessonProgress(LessonProgress lessonProgress, double exerciseProgressPercent)
        {
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonProgress.LessonId);
            if (lesson == null) return;

            double totalProgress = 0.0;
            int totalParts = 0;
            int completedParts = 0;

            totalParts++;
            if (lessonProgress.IsContentViewed == true)
            {
                totalProgress += 1.0;
                completedParts++;
            }

            if (!string.IsNullOrEmpty(lesson.VideoUrl))
            {
                totalParts++;
                if (lessonProgress.IsVideoWatched == true)
                {
                    totalProgress += 1.0;
                    completedParts++;
                }
            }

            if (!string.IsNullOrEmpty(lesson.DocumentUrl))
            {
                totalParts++;
                if (lessonProgress.IsDocumentRead == true)
                {
                    totalProgress += 1.0;
                    completedParts++;
                }
            }

            var exercises = await _unitOfWork.Exercises
                .Query()
                .Where(e => e.LessonID == lessonProgress.LessonId)
                .ToListAsync();

            if (exercises.Any())
            {
                totalParts++;

                double exerciseCompletionRate = exerciseProgressPercent / 100.0;
                totalProgress += exerciseCompletionRate;

                if (exerciseProgressPercent >= 100)
                {
                    completedParts++;
                    lessonProgress.IsPracticeCompleted = true;
                }
            }

            lessonProgress.ProgressPercent = totalParts > 0 ? (totalProgress / totalParts) * 100 : 0;
            lessonProgress.LastUpdated = TimeHelper.GetVietnamTime();

            if (lessonProgress.ProgressPercent >= 100)
            {
                lessonProgress.Status = LearningStatus.Completed;
                lessonProgress.CompletedAt = TimeHelper.GetVietnamTime();
            }
            else if (lessonProgress.ProgressPercent > 0)
            {
                lessonProgress.Status = LearningStatus.InProgress;
            }
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

                await UpdateEnrollmentProgress(unitProgress.EnrollmentId);
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
                .CountAsync(lp => lp.UnitProgress != null && lp.UnitProgress.EnrollmentId == enrollmentId &&
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
            }

            await _unitOfWork.Enrollments.UpdateAsync(enrollment);
        }
        private async Task<bool> IsResubmissionAfterTeacherFail(Guid learnerId, Guid exerciseId)
        {
            var previousTeacherGradedSubmissions = await _unitOfWork.ExerciseSubmissions.Query()
                .Include(es => es.ExerciseGradingAssignments)
                .Where(es => es.LearnerId == learnerId &&
                            es.ExerciseId == exerciseId &&
                            es.ExerciseGradingAssignments.Any())
                .OrderByDescending(es => es.SubmittedAt)
                .ToListAsync();

            return previousTeacherGradedSubmissions.Count > 1;
        }
        #endregion
    }
}
