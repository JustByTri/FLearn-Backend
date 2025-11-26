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
                    var maxLoops = 50;
                    var currentLoop = 0;

                    while (currentLoop < maxLoops)
                    {
                        currentLoop++;

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
            try
            {
                // 1. Validate Manager Check
                var manager = await _unitOfWork.ManagerLanguages.FindAsync(m => m.UserId == userId);
                if (manager == null)
                    return BaseResponse<bool>.Fail(false, "Access denied. Manager privileges required.", 403);

                // 2. Load Submission & Related Data
                var submission = await _unitOfWork.ExerciseSubmissions
                    .Query()
                    .Include(es => es.Learner)
                    .Include(es => es.Exercise)
                        .ThenInclude(e => e.Lesson)
                            .ThenInclude(l => l.CourseUnit)
                                .ThenInclude(cu => cu.Course)
                    .Include(es => es.ExerciseGradingAssignments)
                        .ThenInclude(ega => ega.EarningAllocation)
                    .FirstOrDefaultAsync(es => es.ExerciseSubmissionId == exerciseSubmissionId);

                if (submission == null)
                    return BaseResponse<bool>.Fail(false, "Exercise submission not found", 404);

                if (submission.Exercise?.Lesson?.CourseUnit == null)
                    return BaseResponse<bool>.Fail(false, "Course information not found", 404);

                // 3. Validate Purchase (Giữ nguyên logic của bạn)
                var courseId = submission.Exercise.Lesson.CourseUnit.CourseID;
                var learnerUserId = submission.Learner.UserId;

                var purchase = await _unitOfWork.Purchases.Query()
                    .FirstOrDefaultAsync(p => p.UserId == learnerUserId &&
                                              p.CourseId == courseId &&
                                              p.Status == PurchaseStatus.Completed);

                if (purchase == null)
                    return BaseResponse<bool>.Fail(false, "Valid course purchase not found", 400);

                var hasRefundRequest = await _unitOfWork.RefundRequests.Query()
                    .AnyAsync(r => r.PurchaseId == purchase.PurchasesId &&
                                  (r.Status == RefundRequestStatus.Pending ||
                                   r.Status == RefundRequestStatus.Approved));

                if (hasRefundRequest)
                    return BaseResponse<bool>.Fail(false, "Cannot assign: Course is being refunded.", 400);

                // 4. Validate Assignment State
                var now = TimeHelper.GetVietnamTime();

                var hasActiveAssignment = submission.ExerciseGradingAssignments
                            .Any(a => a.Status == GradingStatus.Assigned && a.DeadlineAt > now);

                if (hasActiveAssignment)
                {
                    var currentAssign = submission.ExerciseGradingAssignments
                        .First(a => a.Status == GradingStatus.Assigned && a.DeadlineAt > now);

                    return BaseResponse<bool>.Fail(false,
                        $"Cannot reassign: This submission is currently assigned to a teacher (ID: {currentAssign.AssignedTeacherId}) and is NOT expired yet.",
                        400);
                }

                var assignmentsToCleanUp = submission.ExerciseGradingAssignments
                            .Where(a => a.Status == GradingStatus.Expired || a.Status == GradingStatus.Assigned)
                            .ToList();

                if (!assignmentsToCleanUp.Any())
                    return BaseResponse<bool>.Fail(false, "No active or expired assignment found to reassign.", 400);

                // 5. Validate New Teacher
                var teacher = await _unitOfWork.TeacherProfiles.Query()
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.TeacherId == teacherId);

                if (teacher == null)
                    return BaseResponse<bool>.Fail(false, "Teacher not found", 404);

                if (!teacher.Status)
                    return BaseResponse<bool>.Fail(false, "Teacher is currently inactive", 400);

                var courseLanguage = submission.Exercise.Lesson.CourseUnit.Course.LanguageId;
                if (teacher.LanguageId != courseLanguage)
                    return BaseResponse<bool>.Fail(false, "Teacher language does not match exercise language", 400);

                decimal oldGradingAmount = 0;

                foreach (var oldAssignment in assignmentsToCleanUp)
                {
                    if (oldAssignment.EarningAllocation != null && oldGradingAmount == 0)
                    {
                        oldGradingAmount = (decimal)oldAssignment.EarningAllocation.ExerciseGradingAmount;
                    }

                    if (oldAssignment.EarningAllocation != null && oldAssignment.EarningAllocation.Status == EarningStatus.Pending)
                    {
                        oldAssignment.EarningAllocation.Status = EarningStatus.Rejected;
                        oldAssignment.EarningAllocation.UpdatedAt = now;
                        await _unitOfWork.TeacherEarningAllocations.UpdateAsync(oldAssignment.EarningAllocation);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    if (oldAssignment.Status == GradingStatus.Assigned)
                    {
                        oldAssignment.Status = GradingStatus.Cancelled;
                        oldAssignment.RevokedAt = now;
                        oldAssignment.RevokedBy = manager.ManagerId;
                        oldAssignment.RevokeReason = "Manager reassigned to another teacher";
                        await _unitOfWork.SaveChangesAsync();
                    }
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
                await _unitOfWork.SaveChangesAsync();

                decimal exerciseGradingAmount = oldGradingAmount;

                if (exerciseGradingAmount == 0)
                {
                    var course = submission.Exercise.Lesson.CourseUnit.Course;
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
                await _unitOfWork.SaveChangesAsync();

                submission.Status = ExerciseSubmissionStatus.PendingTeacherReview;
                submission.TeacherFeedback = string.Empty;
                submission.TeacherScore = 0;

                await _unitOfWork.SaveChangesAsync();

                return BaseResponse<bool>.Success(true, "Exercise successfully reassigned to new teacher");
            }
            catch (Exception ex)
            {
                return BaseResponse<bool>.Error($"Error assigning exercise: {ex.Message}");
            }
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
                var teacher = await _unitOfWork.TeacherProfiles.FindAsync(t => t.UserId == userId);
                if (teacher == null) return PagedResponse<List<ExerciseGradingAssignmentResponse>>.Fail(new object(), "Access denied", 403);

                var query = BuildBaseAssignmentQuery();
                query = query.Where(a => a.AssignedTeacherId == teacher.TeacherId);

                query = ApplyCommonFilters(query, filter);

                if (!string.IsNullOrEmpty(filter.Status))
                {
                    if (Enum.TryParse<GradingStatus>(filter.Status, out var status))
                        query = query.Where(a => a.Status == status);
                }

                query = query.OrderBy(a => a.Status == GradingStatus.Assigned ? 0 : 1)
                      .ThenBy(a => a.DeadlineAt);

                return await ExecutePagingAsync(query, filter);
            }
            catch (Exception ex)
            {
                return PagedResponse<List<ExerciseGradingAssignmentResponse>>.Error($"Error getting assignments: {ex.Message}");
            }
        }
        public async Task<PagedResponse<List<ExerciseGradingAssignmentResponse>>> GetManagerAssignmentsAsync(Guid managerUserId, GradingAssignmentFilterRequest filter)
        {
            var manager = await _unitOfWork.ManagerLanguages.FindAsync(m => m.UserId == managerUserId);
            if (manager == null) return PagedResponse<List<ExerciseGradingAssignmentResponse>>.Fail(new object(), "Manager Access denied", 403);

            var query = BuildBaseAssignmentQuery();

            if (filter.AssignedTeacherId.HasValue)
            {
                query = query.Where(a => a.AssignedTeacherId == filter.AssignedTeacherId.Value);
            }

            query = ApplyCommonFilters(query, filter);

            if (!string.IsNullOrEmpty(filter.Status))
            {
                if (filter.Status.Equals("Expired", StringComparison.OrdinalIgnoreCase))
                {
                    var now = TimeHelper.GetVietnamTime();
                    query = query.Where(a => a.Status == GradingStatus.Expired ||
                                            (a.Status == GradingStatus.Assigned && a.DeadlineAt < now));
                }
                else if (Enum.TryParse<GradingStatus>(filter.Status, out var status))
                {
                    query = query.Where(a => a.Status == status);
                }
            }

            query = query.OrderByDescending(a => a.Status == GradingStatus.Expired)
                         .ThenBy(a => a.DeadlineAt);

            return await ExecutePagingAsync(query, filter);
        }
        public async Task<BaseResponse<GradingFilterOptionsResponse>> GetGradingFilterOptionsAsync(Guid userId)
        {
            IQueryable<ExerciseGradingAssignment> query = _unitOfWork.ExerciseGradingAssignments.Query()
                 .Include(a => a.ExerciseSubmission.Exercise.Lesson.CourseUnit.Course);

            bool isManager = false;

            var manager = await _unitOfWork.ManagerLanguages.FindAsync(m => m.UserId == userId);

            if (manager != null)
            {
                isManager = true;
            }

            if (!isManager)
            {
                var teacher = await _unitOfWork.TeacherProfiles.FindAsync(t => t.UserId == userId);
                if (teacher == null)
                    return BaseResponse<GradingFilterOptionsResponse>.Fail(new object(), "Access denied", 403);
                query = query.Where(a => a.AssignedTeacherId == teacher.TeacherId);
            }

            var courses = await query
                .Select(a => a.ExerciseSubmission.Exercise.Lesson.CourseUnit.Course)
                .Where(c => c != null)
                .Select(c => new FilterOption { Id = c.CourseID, Name = c.Title })
                .Distinct().ToListAsync();

            var exercises = await query
                .Select(a => a.ExerciseSubmission.Exercise)
                .Select(e => new FilterOption { Id = e.ExerciseID, Name = e.Title })
                .Distinct().ToListAsync();

            return BaseResponse<GradingFilterOptionsResponse>.Success(new GradingFilterOptionsResponse
            {
                Courses = courses,
                Exercises = exercises
            });
        }
        public async Task<PagedResponse<List<EligibleTeacherResponse>>> GetEligibleTeachersForReassignmentAsync(EligibleTeacherFilterRequest filter)
        {
            try
            {
                var submission = await _unitOfWork.ExerciseSubmissions.Query()
                    .Include(es => es.ExerciseGradingAssignments)
                    .Include(es => es.Exercise)
                        .ThenInclude(e => e.Lesson)
                            .ThenInclude(l => l.CourseUnit)
                                .ThenInclude(cu => cu.Course)
                                    .ThenInclude(c => c.Teacher)
                    .FirstOrDefaultAsync(es => es.ExerciseSubmissionId == filter.ExerciseSubmissionId);

                if (submission == null)
                    return PagedResponse<List<EligibleTeacherResponse>>.Fail(null, "Submission not found", 404);

                var course = submission.Exercise.Lesson.CourseUnit.Course;
                if (course == null)
                    return PagedResponse<List<EligibleTeacherResponse>>.Fail(null, "Course info not found", 404);

                int requiredProficiencyOrder = submission.Exercise.Lesson.CourseUnit.Course.Teacher.ProficiencyOrder;

                var currentAssignment = submission.ExerciseGradingAssignments
                    .FirstOrDefault(a => a.Status == GradingStatus.Assigned || a.Status == GradingStatus.Expired);
                Guid? excludedTeacherId = currentAssignment?.AssignedTeacherId;

                var query = _unitOfWork.TeacherProfiles.Query()
                    .Include(t => t.User)
                    // ĐK 1: Phải cùng ngôn ngữ với Course
                    .Where(t => t.LanguageId == course.LanguageId)
                    // ĐK 2: Trình độ phải >= trình độ khóa học
                    .Where(t => t.ProficiencyOrder >= requiredProficiencyOrder)
                    // ĐK 3: Giáo viên phải đang Active
                    .Where(t => t.Status == true);

                // ĐK 4: Loại trừ giáo viên cũ (người đã làm expired bài này)
                if (excludedTeacherId.HasValue)
                {
                    query = query.Where(t => t.TeacherId != excludedTeacherId.Value);
                }

                // ĐK 5: Filter theo tên/email (SearchTerm từ PagingRequest)
                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    string term = filter.SearchTerm.ToLower();
                    query = query.Where(t => t.FullName.ToLower().Contains(term) ||
                                             t.Email.ToLower().Contains(term) ||
                                             t.User.FullName.ToLower().Contains(term));
                }

                // 3. Phân trang & Projection
                var totalCount = await query.CountAsync();

                var teachers = await query
                    .OrderByDescending(t => t.AverageRating) // Ưu tiên rating cao
                    .ThenBy(t => t.ProficiencyOrder)         // Sau đó đến trình độ
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(t => new
                    {
                        t.TeacherId,
                        t.UserId,
                        t.FullName,
                        t.Email,
                        t.Avatar,
                        t.ProficiencyCode,
                        t.ProficiencyOrder,
                        t.AverageRating,
                        // Đếm số bài đang assigned (chưa chấm xong) của giáo viên này
                        ActiveAssignmentsCount = t.ExerciseGradingAssignments.Count(a => a.Status == GradingStatus.Assigned)
                    })
                    .ToListAsync();

                var response = teachers.Select(t => new EligibleTeacherResponse
                {
                    TeacherId = t.TeacherId,
                    UserId = t.UserId,
                    FullName = t.FullName,
                    Email = t.Email,
                    Avatar = t.Avatar,
                    ProficiencyCode = t.ProficiencyCode,
                    ProficiencyOrder = t.ProficiencyOrder,
                    AverageRating = t.AverageRating,
                    ActiveAssignmentsCount = t.ActiveAssignmentsCount,
                    // Logic gợi ý: Rating > 4.5 và đang ôm ít hơn 5 bài
                    IsRecommended = t.AverageRating >= 4.5 && t.ActiveAssignmentsCount < 5
                }).ToList();

                return PagedResponse<List<EligibleTeacherResponse>>.Success(
                    response,
                    filter.Page,
                    filter.PageSize,
                    totalCount,
                    "Eligible teachers retrieved successfully");
            }
            catch (Exception ex)
            {
                return PagedResponse<List<EligibleTeacherResponse>>.Error($"Error retrieving teachers: {ex.Message}");
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
                throw new Exception($"[Hangfire Retry] Submission {request.ExerciseSubmissionId} not found yet. Waiting for DB commit.");

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
                                    .ThenInclude(e => e.Lesson)
                                        .ThenInclude(l => l.CourseUnit)
                                .Include(es => es.Learner)
                                    .ThenInclude(l => l.User)
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
                    var learnerId = submission.Learner.UserId;
                    var courseId = submission.Exercise.Lesson.CourseUnit.CourseID;

                    var purchase = await _unitOfWork.Purchases.Query()
                    .FirstOrDefaultAsync(p => p.UserId == learnerId && p.CourseId == courseId);

                    bool isRefundSafe = true;
                    if (purchase != null)
                    {
                        // Nếu Purchase đã Refunded hoặc Failed -> Không trả tiền
                        if (purchase.Status == PurchaseStatus.Refunded ||
                            purchase.Status == PurchaseStatus.Failed ||
                            purchase.Status == PurchaseStatus.Cancelled)
                        {
                            isRefundSafe = false;
                        }
                        else
                        {
                            // Nếu có yêu cầu Refund đang Pending -> Cũng tạm thời KHÔNG trả tiền
                            var pendingRefund = await _unitOfWork.RefundRequests.Query()
                                .AnyAsync(r => r.PurchaseId == purchase.PurchasesId &&
                                              (r.Status == RefundRequestStatus.Pending ||
                                               r.Status == RefundRequestStatus.Approved));

                            if (pendingRefund) isRefundSafe = false;
                        }
                    }
                    else
                    {
                        // Không tìm thấy purchase (lỗi data) -> Không trả tiền cho chắc
                        isRefundSafe = false;
                    }

                    if (isRefundSafe)
                    {
                        // KHỚP LOGIC: Chỉ Approved khi an toàn
                        allocation.Status = EarningStatus.Approved;
                        allocation.ApprovedAt = TimeHelper.GetVietnamTime();
                        allocation.UpdatedAt = TimeHelper.GetVietnamTime();

                        // Lưu ý: Việc gọi Job chuyển tiền sẽ thực hiện SAU KHI save changes thành công
                    }
                    else
                    {
                        // Nếu đang Refund, ta set trạng thái là Cancelled (hoặc Rejected)
                        // Giáo viên đã chấm nhưng do User refund nên Allocation này bị hủy.
                        // (Tuỳ policy bên bạn có trả tiền cho GV hay ko trong case này, 
                        // nhưng để an toàn tài chính thì thường là không hoặc xử lý thủ công).
                        allocation.Status = EarningStatus.Rejected;
                        allocation.UpdatedAt = TimeHelper.GetVietnamTime();

                        Console.WriteLine($"[INFO] Allocation {allocation.AllocationId} cancelled due to Refund/Invalid Purchase status.");
                    }

                    await _unitOfWork.TeacherEarningAllocations.UpdateAsync(allocation);
                }

                await _unitOfWork.SaveChangesAsync();

                if (allocation != null && allocation.Status == EarningStatus.Approved)
                {
                    BackgroundJob.Enqueue<IWalletService>(ws => ws.TransferExerciseGradingFeeToTeacherAsync(allocation.AllocationId));
                }

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
        private IQueryable<ExerciseGradingAssignment> BuildBaseAssignmentQuery()
        {
            return _unitOfWork.ExerciseGradingAssignments.Query()
                .Include(a => a.ExerciseSubmission)
                    .ThenInclude(es => es.Exercise)
                        .ThenInclude(e => e.Lesson)
                            .ThenInclude(l => l.CourseUnit)
                                .ThenInclude(cu => cu.Course)
                .Include(a => a.ExerciseSubmission.Learner.User)
                .Include(a => a.Teacher.User)
                .Include(a => a.EarningAllocation);
        }
        private IQueryable<ExerciseGradingAssignment> ApplyCommonFilters(IQueryable<ExerciseGradingAssignment> query, GradingAssignmentFilterRequest filter)
        {
            if (filter.CourseId.HasValue)
                query = query.Where(a => a.ExerciseSubmission.Exercise.Lesson.CourseUnit.CourseID == filter.CourseId.Value);

            if (filter.ExerciseId.HasValue)
                query = query.Where(a => a.ExerciseSubmission.ExerciseId == filter.ExerciseId.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(a => a.AssignedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(a => a.AssignedAt <= filter.ToDate.Value);

            return query;
        }
        private async Task<PagedResponse<List<ExerciseGradingAssignmentResponse>>> ExecutePagingAsync(IQueryable<ExerciseGradingAssignment> query, GradingAssignmentFilterRequest filter)
        {
            var totalCount = await query.CountAsync();
            var assignments = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var now = TimeHelper.GetVietnamTime();
            var data = assignments.Select(a => new ExerciseGradingAssignmentResponse
            {
                AssignmentId = a.GradingAssignmentId,
                ExerciseSubmissionId = a.ExerciseSubmissionId,
                LearnerId = a?.ExerciseSubmission?.LearnerId ?? Guid.Empty,
                LearnerName = a?.ExerciseSubmission?.Learner?.User?.FullName ?? a?.ExerciseSubmission?.Learner?.User?.UserName ?? string.Empty,
                AudioUrl = a.ExerciseSubmission.AudioUrl,
                ExerciseId = a.ExerciseSubmission.ExerciseId,
                ExerciseType = a.ExerciseSubmission.Exercise.Type.ToString(),
                LessonId = a.ExerciseSubmission.Exercise.LessonID,
                LessonTitle = a.ExerciseSubmission.Exercise.Lesson?.Title ?? "Unknown",
                CourseId = a.ExerciseSubmission.Exercise.Lesson?.CourseUnit?.CourseID,
                AssignedTeacherId = a.AssignedTeacherId,
                AssignedTeacherName = a.Teacher?.FullName ?? a.Teacher?.User?.FullName,
                AIScore = a.ExerciseSubmission.AIScore,
                AIFeedback = a.ExerciseSubmission.AIFeedback,
                EarningStatus = a.EarningAllocation?.Status.ToString() ?? "N/A",
                EarningAmount = a.EarningAllocation?.ExerciseGradingAmount ?? 0,
                CompletedAt = a.CompletedAt?.ToString("dd-MM-yyyy HH:mm"),
                StartedAt = a.StartedAt?.ToString("dd-MM-yyyy HH:mm"),
                Feedback = a.Feedback,
                GradingStatus = a.ExerciseSubmission.Status.ToString(),
                ExerciseTitle = a.ExerciseSubmission.Exercise.Title,
                CourseName = a.ExerciseSubmission.Exercise.Lesson?.CourseUnit?.Course?.Title ?? "Unknown",
                Status = a.Status.ToString(),
                AssignedAt = a.AssignedAt.ToString("dd-MM-yyyy HH:mm"),
                Deadline = a.DeadlineAt.ToString("dd-MM-yyyy HH:mm"),
                IsOverdue = a.DeadlineAt < now && a.Status == GradingStatus.Expired,
                HoursRemaining = (a.Status == GradingStatus.Assigned)
                    ? (a.DeadlineAt > now ? (int)(a.DeadlineAt - now).TotalHours : 0)
                    : 0,

                FinalScore = a.FinalScore
            }).ToList();

            return PagedResponse<List<ExerciseGradingAssignmentResponse>>.Success(data, filter.Page, filter.PageSize, totalCount);
        }
        #endregion
    }
}
