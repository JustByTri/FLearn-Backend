using BLL.IServices.Assessment;
using BLL.IServices.ProgressTracking;
using Common.DTO.ApiResponse;
using Common.DTO.ExerciseGrading.Request;
using Common.DTO.ExerciseGrading.Response;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.ProgressTracking
{
    public class ExerciseGradingService : IExerciseGradingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAssessmentService _assessmentService;
        public ExerciseGradingService(IUnitOfWork unitOfWork, IAssessmentService assessmentService)
        {
            _unitOfWork = unitOfWork;
            _assessmentService = assessmentService;
        }
        public Task<BaseResponse<bool>> CheckAndReassignExpiredAssignmentsAsync()
        {
            throw new Exception();
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
        public async Task<BaseResponse<List<ExerciseGradingAssignmentResponse>>> GetTeacherAssignmentsAsync(Guid userId, GradingAssignmentFilterRequest filter)
        {
            try
            {
                var teacher = await _unitOfWork.TeacherProfiles.FindAsync(t => t.UserId == userId);
                if (teacher == null)
                    return BaseResponse<List<ExerciseGradingAssignmentResponse>>.Fail(new object(), "Access denied", 403);

                var query = _unitOfWork.ExerciseGradingAssignments
                    .Query()
                    .Include(a => a.ExerciseSubmission)
                    .ThenInclude(es => es.Exercise)
                    .Include(a => a.ExerciseSubmission)
                    .ThenInclude(es => es.Learner)
                    .ThenInclude(l => l.User)
                    .Where(a => a.AssignedTeacherId == teacher.TeacherId);

                if (!string.IsNullOrEmpty(filter.Status) &&
                    Enum.TryParse<GradingStatus>(filter.Status, out var statusFilter))
                {
                    query = query.Where(a => a.Status == statusFilter);
                }

                if (filter.FromDate.HasValue)
                {
                    query = query.Where(a => a.AssignedAt >= filter.FromDate.Value);
                }

                if (filter.ToDate.HasValue)
                {
                    query = query.Where(a => a.AssignedAt <= filter.ToDate.Value);
                }

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
                    AudioUrl = a.ExerciseSubmission.AudioUrl,
                    AIScore = a.ExerciseSubmission.AIScore,
                    AIFeedback = a.ExerciseSubmission.AIFeedback,
                    Status = a.Status.ToString(),
                    AssignedAt = a.AssignedAt.ToString("dd-MM-yyyy"),
                    Deadline = a.DeadlineAt.ToString("dd-MM-yyyy"),
                    IsOverdue = a.DeadlineAt < now && a.Status == GradingStatus.Assigned,
                    HoursRemaining = a.Status == GradingStatus.Assigned ?
                        (int)(a.DeadlineAt - now).TotalHours : 0
                }).ToList();

                return BaseResponse<List<ExerciseGradingAssignmentResponse>>.Success(response, "Assignments retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<List<ExerciseGradingAssignmentResponse>>.Error($"Error getting assignments: {ex.Message}");
            }
        }
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
                if (allocation != null)
                {
                    allocation.Status = EarningStatus.Approved;
                    allocation.UpdatedAt = TimeHelper.GetVietnamTime();
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

                if (passedExercises == totalExercises && totalExercises > 0)
                {
                    await UpdateUnitProgress(lessonProgress.UnitProgressId);
                }
            }
        }
        private async Task UpdateOverallLessonProgress(LessonProgress lessonProgress, double exerciseProgressPercent)
        {
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonProgress.LessonId);
            if (lesson == null) return;

            double totalProgress = 0.0;
            int completedActivities = 0;
            int totalActivities = 1;

            if (lessonProgress.IsContentViewed == true)
            {
                totalProgress += 0.5;
                completedActivities++;
            }

            if (lesson.VideoUrl != null)
            {
                totalActivities++;
                if (lessonProgress.IsVideoWatched == true)
                {
                    totalProgress += 0.2;
                    completedActivities++;
                }
            }

            if (lesson.DocumentUrl != null)
            {
                totalActivities++;
                if (lessonProgress.IsDocumentRead == true)
                {
                    totalProgress += 0.2;
                    completedActivities++;
                }
            }

            if (lesson.Exercises.Any())
            {
                totalActivities++;
                totalProgress += (exerciseProgressPercent / 100) * 0.3;
                if (exerciseProgressPercent >= 100)
                {
                    completedActivities++;
                    lessonProgress.IsPracticeCompleted = true;
                }
            }

            lessonProgress.ProgressPercent = Math.Min(totalProgress * 100, 100);
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
        #endregion
    }
}
