using BLL.IServices.Gamification;
using BLL.IServices.ProgressTracking;
using BLL.IServices.Upload;
using Common.DTO.ApiResponse;
using Common.DTO.ExerciseGrading.Request;
using Common.DTO.ExerciseSubmission.Response;
using Common.DTO.Paging.Response;
using Common.DTO.ProgressTracking.Request;
using Common.DTO.ProgressTracking.Response;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Hangfire;
using Microsoft.EntityFrameworkCore;

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

                if (currentUnitProgress == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "No active unit progress found", 404);

                var currentLessonProgress = await _unitOfWork.LessonProgresses
                    .Query()
                    .FirstOrDefaultAsync(lp => lp.UnitProgressId == currentUnitProgress.UnitProgressId &&
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
                    .FirstOrDefaultAsync(e => e.LearnerId == learner.LearnerLanguageId && e.CourseId == course.CourseID);

                if (enrollment == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Enrollment not found", 404);

                var unitProgress = await _unitOfWork.UnitProgresses.FindAsync(up => up.EnrollmentId == enrollment.EnrollmentID && up.CourseUnitId == request.UnitId);

                if (unitProgress == null)
                {
                    unitProgress = new UnitProgress
                    {
                        UnitProgressId = Guid.NewGuid(),
                        EnrollmentId = enrollment.EnrollmentID,
                        CourseUnitId = unit.CourseUnitID,
                        Status = LearningStatus.InProgress,
                        StartedAt = TimeHelper.GetVietnamTime(),
                        LastUpdated = TimeHelper.GetVietnamTime()
                    };
                    await _unitOfWork.UnitProgresses.CreateAsync(unitProgress);
                }

                var lessonProgress = await _unitOfWork.LessonProgresses.FindAsync(lp => lp.UnitProgressId == unitProgress.UnitProgressId && lp.LessonId == request.LessonId);

                if (lessonProgress == null)
                {
                    lessonProgress = new LessonProgress
                    {
                        LessonProgressId = Guid.NewGuid(),
                        UnitProgressId = unitProgress.UnitProgressId,
                        LessonId = lesson.LessonID,
                        Status = LearningStatus.InProgress,
                        StartedAt = TimeHelper.GetVietnamTime(),
                        LastUpdated = TimeHelper.GetVietnamTime()
                    };
                    await _unitOfWork.LessonProgresses.CreateAsync(lessonProgress);

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

                if (user == null || !user.Status || !user.IsEmailConfirmed)
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
                    .Include(c => c.Language)
                    .Where(c => c.CourseID == unit.CourseID).FirstOrDefaultAsync();

                if (course == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Course not found", 404);

                var learner = await _unitOfWork.LearnerLanguages
                    .FindAsync(l => l.UserId == user.UserID && l.LanguageId == course.LanguageId);

                if (learner == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Access denied", 403);

                var enrollment = await _unitOfWork.Enrollments
                    .FindAsync(e => e.LearnerId == learner.LearnerLanguageId && e.CourseId == course.CourseID);

                if (enrollment == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Enrollment not found", 404);

                var unitProgress = await _unitOfWork.UnitProgresses
                    .FindAsync(up => up.EnrollmentId == enrollment.EnrollmentID && up.CourseUnitId == unit.CourseUnitID);

                if (unitProgress == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Unit progress not found", 404);

                var lessonProgress = await _unitOfWork.LessonProgresses
                    .FindAsync(lp => lp.UnitProgressId == unitProgress.UnitProgressId && lp.LessonId == lesson.LessonID);
                if (lessonProgress == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Lesson progress not found", 404);

                /*
                 * Kiểm tra người dùng có thể nộp bài tập không vì một ngày chỉ được nộp tối đa 3 lần cho chính bài tập đó.
                 */
                var canSubmit = await CanSubmitExerciseAsync(learner.LearnerLanguageId, request.ExerciseId);
                if (!canSubmit)
                {
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(
                        new
                        {
                            MaxAttempts = 3,
                            CoolDownHours = 24
                        },
                        @"**Giới hạn nộp bài hàng ngày**
                            Số lần đã nộp: 3/3
                            Trạng thái: ĐẠT TỐI ĐA
                            Thời gian reset: 00:00
                            Bạn đã hoàn thành tất cả lượt nộp bài cho phép trong ngày hôm nay. Tính năng này giúp tối ưu hóa quá trình học tập.
                            Hệ thống sẽ tự động reset vào đầu ngày mai.",
                        429);
                }

                bool allowTeacherGrading = false;

                bool hasTeacherGrading = false;

                bool requiresCheck = exercise.Type is SpeakingExerciseType.StoryTelling or SpeakingExerciseType.Debate;

                if (course.GradingType == GradingType.AIAndTeacher && requiresCheck)
                {
                    hasTeacherGrading = await HasTeacherGrading(learner.LearnerLanguageId, request.ExerciseId);

                    allowTeacherGrading = await AllowTeacherGrading(learner.LearnerLanguageId, request.ExerciseId);
                }

                /*
                 * Tải âm thanh trong bài nộp của người học lên cloudinary
                 */
                string audioUrl = string.Empty;
                string publicId = string.Empty;

                var uploadResult = await _cloudinaryService.UploadAudioAsync(request.Audio);
                if (uploadResult == null)
                    return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Upload audio failed", 404);
                else
                {
                    audioUrl = uploadResult.Url;
                    publicId = uploadResult.PublicId;
                }


                // Determine grading type and allocate funds
                bool isTeacherRequired = (exercise.Type == SpeakingExerciseType.StoryTelling ||
                         exercise.Type == SpeakingExerciseType.Debate) &&
                         course.GradingType == GradingType.AIAndTeacher &&
                         allowTeacherGrading;

                double aiPercentage = 100;
                double teacherPercentage = 0;
                decimal exerciseGradingAmount = 0;
                bool canAssignToTeacher = isTeacherRequired;

                if (allowTeacherGrading == false)
                {
                    isTeacherRequired = false;
                    canAssignToTeacher = false;
                    aiPercentage = 100;
                    teacherPercentage = 0;
                }

                if (isTeacherRequired == true && hasTeacherGrading == false)
                {
                    var purchase = await _unitOfWork.Purchases.FindAsync(
                        p => p.UserId == userId &&
                        p.CourseId == course.CourseID &&
                        p.Status == PurchaseStatus.Completed);

                    if (purchase != null)
                    {
                        //Tất cả bài tập cần giáo viên chấm của khóa học này.
                        var teacherGradingExercises = await _unitOfWork.Exercises.Query()
                            .CountAsync(e => e.Lesson != null &&
                                           e.Lesson.CourseUnit != null &&
                                           e.Lesson.CourseUnit.CourseID == course.CourseID &&
                                           (e.Type == SpeakingExerciseType.StoryTelling ||
                                            e.Type == SpeakingExerciseType.Debate));

                        if (teacherGradingExercises > 0)
                        {
                            /* 
                             * Số tiền cho hệ thống là 10% số tiền của khóa học 
                             * Còn lại 90% là tiền tạo khóa học và tiền chấm bài tập.
                             */
                            decimal amountForDistribution = purchase.FinalAmount * 0.9m;

                            /* 
                             * Số tiền 35% của 90% là tiền chấm bài 
                             * sẽ được chia đều cho tất cả bài tập cần giáo viên chấm
                             */
                            decimal gradingFeeAmount = amountForDistribution * 0.35m;
                            exerciseGradingAmount = gradingFeeAmount / teacherGradingExercises;

                            /*
                             * Nếu khóa học là loại cần cả giáo viên và AI chấm
                             * Thuộc loại bài tập cần giáo viên chấm (Kể chuyện và Tranh luận)
                             * Bài nộp của người học cho chính bài tập đó chưa được một giáo viên nào chấm
                             * Thì % AI chấm là 30% và % Giáo viên chấm là 70%.
                             */
                            aiPercentage = DefaultAIPercentage;
                            teacherPercentage = DefaultTeacherPercentage;
                        }
                    }
                    else
                    {
                        canAssignToTeacher = false;
                        aiPercentage = 100;
                        teacherPercentage = 0;
                    }
                }

                /*
                 * Nếu khóa học thuộc loại chỉ cần AI chấm
                 * Thuộc loại bài tập (Lặp lại theo mẫu/ Mô tả tranh)
                 * Thì % AI chấm sẽ là 100%
                 */
                if (exercise.Type == SpeakingExerciseType.RepeatAfterMe ||
                    exercise.Type == SpeakingExerciseType.PictureDescription || course.GradingType == GradingType.AIOnly)
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
                    AIFeedback = @"Bài nộp của bạn đang được AI xem xét và đánh giá. Kết quả sẽ được cập nhật khi quá trình hoàn tất.",
                    TeacherFeedback = isTeacherRequired ? "Bài nộp của bạn đang chờ giáo viên xem xét và đánh giá." : string.Empty,
                    Status = ExerciseSubmissionStatus.PendingAIReview,
                    SubmittedAt = TimeHelper.GetVietnamTime(),
                };

                await _unitOfWork.ExerciseSubmissions.CreateAsync(submission);

                /*
                 * Nếu là lần đầu nộp bài thì hệ thống sẽ giao cho chính giáo viên tạo ra khóa học chấm
                 * Đồng thời người giáo viên đó sẽ nhận được một khoản tiền sau khi chấm xong (dù Passed hay Failed).
                 */
                if (canAssignToTeacher && course?.TeacherId != null && exerciseGradingAmount > 0)
                {
                    var gradingAssignment = new ExerciseGradingAssignment
                    {
                        GradingAssignmentId = Guid.NewGuid(),
                        ExerciseSubmissionId = submission.ExerciseSubmissionId,
                        AssignedTeacherId = course.TeacherId,
                        StartedAt = TimeHelper.GetVietnamTime(),
                        AssignedAt = TimeHelper.GetVietnamTime(),
                        Status = GradingStatus.Assigned,
                        DeadlineAt = TimeHelper.GetVietnamTime().AddDays(2),
                        CreatedAt = TimeHelper.GetVietnamTime()
                    };

                    await _unitOfWork.ExerciseGradingAssignments.CreateAsync(gradingAssignment);

                    if (!hasTeacherGrading && exerciseGradingAmount > 0)
                    {
                        var earningAllocation = new TeacherEarningAllocation
                        {
                            AllocationId = Guid.NewGuid(),
                            TeacherId = course.TeacherId,
                            GradingAssignmentId = gradingAssignment.GradingAssignmentId,
                            ExerciseGradingAmount = exerciseGradingAmount,
                            EarningType = EarningType.ExerciseGrading,
                            Status = EarningStatus.Pending,
                            CreatedAt = TimeHelper.GetVietnamTime(),
                            UpdatedAt = TimeHelper.GetVietnamTime()
                        };

                        await _unitOfWork.TeacherEarningAllocations.CreateAsync(earningAllocation);
                    }
                }

                // XP: submitting an exercise earns small XP
                await _gamificationService.AwardXpAsync(learner, 10, "Submit exercise");

                lessonProgress.LastUpdated = TimeHelper.GetVietnamTime();
                await _unitOfWork.SaveChangesAsync();

                var gradingType = isTeacherRequired ? GradingType.AIAndTeacher.ToString() : GradingType.AIOnly.ToString();

                /*
                 * Tạo 1 yêu cầu để bên exercise grading service đánh giá
                 * Gồm ID bài nộp
                 * Loại chấm là AI chấm hay cả AI và Giáo viên cùng chấm điểm
                 * Ngôn ngữ của khóa học
                 * Đường dẫn file nói của người học cho chính bài tập đó
                 */
                AssessmentRequest assessmentRequest = new AssessmentRequest
                {
                    ExerciseSubmissionId = submission.ExerciseSubmissionId,
                    AudioUrl = audioUrl,
                    LanguageCode = (course != null) ? course.Language.LanguageCode : "en",
                    GradingType = gradingType
                };

                // Tự động chấm điểm ngầm
                BackgroundJob.Enqueue(() => _exerciseGradingService.ProcessAIGradingAsync(assessmentRequest));
                //await _exerciseGradingService.ProcessAIGradingAsync(assessmentRequest);

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

                var unitProgress = await _unitOfWork.UnitProgresses.FindAsync(up => up.EnrollmentId == enrollment.EnrollmentID && up.CourseUnitId == unit.CourseUnitID);
                if (unitProgress == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Unit progress not found", 404);

                var lessonProgress = await _unitOfWork.LessonProgresses.Query()
                .Include(lp => lp.UnitProgress)
                    .ThenInclude(up => up.Enrollment)
                        .ThenInclude(e => e.Learner)
                .Where(lp => lp.UnitProgressId == unitProgress.UnitProgressId && lp.LessonId == lesson.LessonID).FirstOrDefaultAsync();

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
            if (submission?.Exercise?.Lesson?.CourseUnit?.Course == null)
            {
                return new ExerciseSubmissionDetailResponse
                {
                    ExerciseSubmissionId = submission?.ExerciseSubmissionId ?? Guid.Empty,
                    Status = submission?.Status.ToString() ?? "Unknown",
                };
            }

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
                FinalScore = submission.FinalScore,
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

            // Count total available activities
            int totalActivities = 1; // Content is always required

            if (lesson.VideoUrl != null)
                totalActivities++;

            if (lesson.DocumentUrl != null)
                totalActivities++;

            var exercises = await _unitOfWork.Exercises
                .Query()
                .Where(e => e.LessonID == lessonProgress.LessonId)
                .ToListAsync();

            if (exercises.Any())
                totalActivities++;

            // Calculate equal weight for each activity
            double activityWeight = 1.0 / totalActivities;

            // Content is mandatory (always counts)
            if (lessonProgress.IsContentViewed == true)
            {
                progress += activityWeight;
                completedActivities++;
            }

            // Video is optional
            if (lesson.VideoUrl != null)
            {
                if (lessonProgress.IsVideoWatched == true)
                {
                    progress += activityWeight;
                    completedActivities++;
                }
            }

            // Document is optional
            if (lesson.DocumentUrl != null)
            {
                if (lessonProgress.IsDocumentRead == true)
                {
                    progress += activityWeight;
                    completedActivities++;
                }
            }

            // Exercises are optional
            if (exercises.Any())
            {
                var completedExercises = await _unitOfWork.ExerciseSubmissions
                    .Query()
                    .Where(es => es.LessonProgressId == lessonProgress.LessonProgressId &&
                                es.Status == ExerciseSubmissionStatus.Passed)
                    .Select(es => es.ExerciseId)
                    .Distinct()
                    .CountAsync();

                // Calculate exercise progress based on actual completed exercises
                double exerciseProgress = (double)completedExercises / exercises.Count;
                progress += (activityWeight * exerciseProgress);

                // Count as completed activity only if ALL exercises are completed
                if (completedExercises == exercises.Count)
                {
                    completedActivities++;
                    lessonProgress.IsPracticeCompleted = true;
                }
            }

            lessonProgress.ProgressPercent = progress * 100;
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
        private async Task<bool> CanSubmitExerciseAsync(Guid learnerId, Guid exerciseId)
        {
            var today = TimeHelper.GetVietnamTime().Date;

            //Đếm số lượng bài tập nộp trong một ngày của một người học cụ thể.
            var todaysSubmissions = await _unitOfWork.ExerciseSubmissions.Query()
                .Where(es => es.LearnerId == learnerId &&
                             es.ExerciseId == exerciseId &&
                             es.SubmittedAt >= today).CountAsync();

            //Nếu số lượng bài nộp lớn hơn hoặc bằng 3 thì người học không thể nộp được nữa và phải chờ đến ngày mai.
            if (todaysSubmissions >= 3)
                return false;

            return true;
        }
        private async Task<bool> HasTeacherGrading(Guid learnerId, Guid exerciseId)
        {
            //Kiểm tra bài tập đã có giáo viên chấm hay chưa
            return await _unitOfWork.ExerciseSubmissions.Query()
                .AnyAsync(es => es.LearnerId == learnerId &&
                               es.ExerciseId == exerciseId &&
                               es.ExerciseGradingAssignments.Any(ega =>
                                   ega.Status != GradingStatus.Expired));
        }
        private async Task<bool> AllowTeacherGrading(Guid learnerId, Guid exerciseId)
        {
            // Đếm số bài nộp đã được giao cho giáo viên chấm (chưa hết hạn)
            var gradedSubmissionsCount = await _unitOfWork.ExerciseSubmissions.Query()
                .Where(es => es.LearnerId == learnerId &&
                            es.ExerciseId == exerciseId &&
                            es.ExerciseGradingAssignments.Any(ega =>
                                ega.Status != GradingStatus.Expired))
                .CountAsync();

            // Cho phép nếu số bài đã giao chấm < 2 (lần 1 và lần 2)
            return gradedSubmissionsCount < 2;
        }
        #endregion
    }
}
