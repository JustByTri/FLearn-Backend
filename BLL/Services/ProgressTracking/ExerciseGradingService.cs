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
                    FinalScore = (submission.AIScore + submission.TeacherScore) / 2,
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

            var submission = await _unitOfWork.ExerciseSubmissions
                .Query()
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
                submission.AIFeedback = aiEvaluation.Feedback;
                submission.Status = ExerciseSubmissionStatus.AIGraded;

                if (submission.AIScore >= exercise.PassScore)
                {
                    submission.IsPassed = true;

                    if (request.GradingType == GradingType.AIOnly.ToString())
                    {
                        submission.Status = ExerciseSubmissionStatus.Passed;
                        submission.ReviewedAt = TimeHelper.GetVietnamTime();
                    }
                    else if (request.GradingType == GradingType.AIAndTeacher.ToString())
                    {
                        submission.Status = ExerciseSubmissionStatus.PendingTeacherReview;
                    }
                }
                else
                {
                    submission.IsPassed = false;
                    if (request.GradingType == GradingType.AIOnly.ToString())
                    {
                        submission.Status = ExerciseSubmissionStatus.Failed;
                        submission.ReviewedAt = TimeHelper.GetVietnamTime();
                    }
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
                var teacher = await _unitOfWork.TeacherProfiles.FindAsync(t => t.UserId == userId);
                if (teacher == null)
                    return BaseResponse<bool>.Fail(new object(), "Access denied", 403);

                var submission = await _unitOfWork.ExerciseSubmissions
                    .Query()
                    .Include(es => es.Exercise)
                    .Include(es => es.ExerciseGradingAssignments)
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
                submission.ReviewedAt = TimeHelper.GetVietnamTime();


                double finalScore = 0;

                if (submission?.AIScore != null)
                {
                    finalScore = (submission.AIScore + score) / 2;
                }
                else
                {
                    finalScore = score;
                }

                submission.IsPassed = finalScore >= submission.Exercise.PassScore;
                submission.Status = submission.IsPassed == true ?
                    ExerciseSubmissionStatus.Passed : ExerciseSubmissionStatus.Failed;

                assignment.Status = GradingStatus.Returned;
                assignment.CompletedAt = TimeHelper.GetVietnamTime();
                assignment.FinalScore = score;
                assignment.Feedback = feedback;

                await _unitOfWork.ExerciseSubmissions.UpdateAsync(submission);
                await _unitOfWork.ExerciseGradingAssignments.UpdateAsync(assignment);

                var earningAllocation = new TeacherEarningAllocation
                {
                    AllocationId = Guid.NewGuid(),
                    TeacherId = teacher.TeacherId,
                    GradingAssignmentId = assignment.GradingAssignmentId,
                    ExerciseGradingAmount = CalculateGradingFee(submission.Exercise.Difficulty.ToString()),
                    EarningType = EarningType.ExerciseGrading,
                    ApprovedAt = TimeHelper.GetVietnamTime(),
                    Status = EarningStatus.Approved,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
                };

                await _unitOfWork.TeacherEarningAllocations.CreateAsync(earningAllocation);

                await _unitOfWork.SaveChangesAsync();

                await UpdateLessonProgressAfterGrading(submission);

                return BaseResponse<bool>.Success(true, "Teacher grading completed successfully");
            });
        }
        #region
        private decimal CalculateGradingFee(string difficultyLevel)
        {
            return difficultyLevel?.ToUpper() switch
            {
                "EASY" => 5000,
                "MEDIUM" => 8000,
                "HARD" => 12000,
                "ADVANCED" => 15000,
                _ => 5000
            };
        }
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
                    .Count(es => es.IsPassed == true);

                var totalExercises = lessonProgress.Lesson.Exercises.Count;

                if (passedExercises == totalExercises && totalExercises > 0)
                {
                    lessonProgress.LastUpdated = TimeHelper.GetVietnamTime();
                    await _unitOfWork.LessonProgresses.UpdateAsync(lessonProgress);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }
        #endregion
    }
}
