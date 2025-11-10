using BLL.IServices.ProgressTracking;
using BLL.IServices.Upload;
using Common.DTO.ApiResponse;
using Common.DTO.ExerciseGrading.Request;
using Common.DTO.ExerciseSubmission.Response;
using Common.DTO.ProgressTracking.Request;
using Common.DTO.ProgressTracking.Response;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.ProgressTracking
{
    public class ProgressTrackingService : IProgressTrackingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IExerciseGradingService _exerciseGradingService;
        private readonly ICloudinaryService _cloudinaryService;
        public ProgressTrackingService(IUnitOfWork unitOfWork, IExerciseGradingService exerciseGradingService, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _exerciseGradingService = exerciseGradingService;
            _cloudinaryService = cloudinaryService;
        }
        public async Task<BaseResponse<List<ExerciseSubmissionDetailResponse>>> GetMySubmissionsAsync(Guid userId, Guid? courseId, Guid? lessonId, string? status)
        {
            try
            {
                var learner = await _unitOfWork.LearnerLanguages
                    .Query()
                    .FirstOrDefaultAsync(l => l.UserId == userId);

                if (learner == null)
                    return BaseResponse<List<ExerciseSubmissionDetailResponse>>.Fail(new List<ExerciseSubmissionDetailResponse>(), "Learner not found", 403);

                var query = _unitOfWork.ExerciseSubmissions
                    .Query()
                    .Where(es => es.LearnerId == learner.LearnerLanguageId)
                    .Include(es => es.Exercise)
                    .ThenInclude(e => e.Lesson)
                    .ThenInclude(l => l.CourseUnit)
                    .ThenInclude(u => u.Course)
                    .Include(es => es.ExerciseGradingAssignments)
                    .ThenInclude(ega => ega.Teacher)
                    .ThenInclude(t => t.User)
                    .AsQueryable();

                if (courseId.HasValue)
                {
                    query = query.Where(es => es.Exercise.Lesson.CourseUnit.CourseID == courseId.Value);
                }

                if (lessonId.HasValue)
                {
                    query = query.Where(es => es.Exercise.LessonID == lessonId.Value);
                }

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<ExerciseSubmissionStatus>(status, out var statusFilter))
                {
                    query = query.Where(es => es.Status == statusFilter);
                }

                var submissions = await query
                    .OrderByDescending(es => es.SubmittedAt)
                    .Take(50)
                    .ToListAsync();

                var responses = submissions.Select(es => MapToDetailResponse(es)).ToList();

                return BaseResponse<List<ExerciseSubmissionDetailResponse>>.Success(responses, "Submissions retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<List<ExerciseSubmissionDetailResponse>>.Error($"Error retrieving submissions: {ex.Message}");
            }
        }
        public async Task<BaseResponse<ExerciseSubmissionDetailResponse>> GetSubmissionDetailAsync(Guid userId, Guid submissionId)
        {
            try
            {
                var submission = await _unitOfWork.ExerciseSubmissions
                    .Query()
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

                var learner = await _unitOfWork.LearnerLanguages
                    .Query()
                    .FirstOrDefaultAsync(l => l.LearnerLanguageId == submission.LearnerId);

                if (learner == null || learner.UserId != userId)
                    return BaseResponse<ExerciseSubmissionDetailResponse>.Fail(new ExerciseSubmissionDetailResponse(), "Access denied", 403);

                var response = MapToDetailResponse(submission);

                return BaseResponse<ExerciseSubmissionDetailResponse>.Success(response, "Submission detail retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<ExerciseSubmissionDetailResponse>.Error($"Error retrieving submission detail: {ex.Message}");
            }
        }
        public async Task<BaseResponse<List<ExerciseSubmissionHistoryResponse>>> GetExerciseSubmissionsHistoryAsync(Guid userId, Guid exerciseId)
        {
            try
            {
                var learner = await _unitOfWork.LearnerLanguages
                    .Query()
                    .FirstOrDefaultAsync(l => l.UserId == userId);

                if (learner == null)
                    return BaseResponse<List<ExerciseSubmissionHistoryResponse>>.Fail(new List<ExerciseSubmissionHistoryResponse>(), "Learner not found", 403);

                var submissions = await _unitOfWork.ExerciseSubmissions
                    .Query()
                    .Where(es => es.LearnerId == learner.LearnerLanguageId && es.ExerciseId == exerciseId)
                    .OrderByDescending(es => es.SubmittedAt)
                    .ToListAsync();

                var responses = submissions.Select(es => new ExerciseSubmissionHistoryResponse
                {
                    ExerciseSubmissionId = es.ExerciseSubmissionId,
                    SubmittedAt = es.SubmittedAt.ToString("dd-MM-yyyy HH:mm"),
                    Status = es.Status.ToString(),
                    FinalScore = (es.TeacherScore == 0) ? es.AIScore : (es.AIScore + es.TeacherScore) / 2,
                    IsPassed = es.IsPassed,
                    AudioUrl = es.AudioUrl,
                    TeacherFeedback = es.TeacherFeedback
                }).ToList();

                return BaseResponse<List<ExerciseSubmissionHistoryResponse>>.Success(responses, "Submission history retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<List<ExerciseSubmissionHistoryResponse>>.Error($"Error retrieving submission history: {ex.Message}");
            }
        }
        public async Task<BaseResponse<ProgressTrackingResponse>> GetCurrentProgressAsync(Guid userId, Guid courseId)
        {
            try
            {
                var learner = await _unitOfWork.LearnerLanguages
                    .Query()
                    .FirstOrDefaultAsync(l => l.UserId == userId);
                if (learner == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Learner not found", 404);

                var enrollment = await _unitOfWork.Enrollments
                    .Query()
                    .FirstOrDefaultAsync(e => e.LearnerId == learner.LearnerLanguageId && e.CourseId == courseId);

                if (enrollment == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Enrollment not found", 404);

                var currentUnitProgress = await _unitOfWork.UnitProgresses
                    .Query()
                    .FirstOrDefaultAsync(up => up.EnrollmentId == enrollment.EnrollmentID &&
                                             up.CourseUnitId == enrollment.CurrentUnitId);

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
                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == userId);
                if (learner == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Access denied", 403);

                var enrollment = await _unitOfWork.Enrollments
                    .Query()
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.LearnerId == learner.LearnerLanguageId &&
                                            e.Course.CourseUnits.Any(u => u.CourseUnitID == request.UnitId));

                if (enrollment == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Enrollment not found", 404);

                var unitProgress = await _unitOfWork.UnitProgresses
                    .Query()
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

            var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == userId);
            if (learner == null)
                return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Access denied", 403);

            var exercise = await _unitOfWork.Exercises.GetByIdAsync(request.ExerciseId);
            if (exercise == null)
                return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Exercise not found", 404);

            var lessonProgress = await _unitOfWork.LessonProgresses
                .Query()
                .Include(lp => lp.UnitProgress)
                .ThenInclude(up => up.Enrollment)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.Language)
                .FirstOrDefaultAsync(lp => lp.LessonId == exercise.LessonID &&
                                         lp.UnitProgress != null &&
                                         lp.UnitProgress.Enrollment != null &&
                                         lp.UnitProgress.Enrollment.LearnerId == learner.LearnerLanguageId &&
                                         lp.Status != LearningStatus.Completed);

            if (lessonProgress == null)
                return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Lesson progress not found", 404);

            var uploadResult = await _cloudinaryService.UploadAudioAsync(request.Audio);
            if (uploadResult == null)
                return BaseResponse<ExerciseSubmissionResponse>.Fail(new object(), "Upload audio failed", 404);

            var submission = new ExerciseSubmission
            {
                ExerciseSubmissionId = Guid.NewGuid(),
                LearnerId = learner.LearnerLanguageId,
                ExerciseId = request.ExerciseId,
                LessonProgressId = lessonProgress.LessonProgressId,
                AudioUrl = uploadResult.Url,
                AudioPublicId = uploadResult.PublicId,
                AIFeedback = "Pending AI evaluation",
                TeacherFeedback = "Pending Teacher evaluation",
                Status = ExerciseSubmissionStatus.PendingAIReview,
                SubmittedAt = TimeHelper.GetVietnamTime()
            };
            await _unitOfWork.ExerciseSubmissions.CreateAsync(submission);

            var course = lessonProgress.UnitProgress?.Enrollment.Course;

            bool isTeacherRequired = exercise.Type == SpeakingExerciseType.StoryTelling ||
                       exercise.Type == SpeakingExerciseType.Debate;

            if (isTeacherRequired)
            {
                var gradingAssignment = new ExerciseGradingAssignment
                {
                    GradingAssignmentId = Guid.NewGuid(),
                    ExerciseSubmissionId = submission.ExerciseSubmissionId,
                    AssignedTeacherId = course?.TeacherId,
                    AssignedAt = TimeHelper.GetVietnamTime(),
                    DeadlineAt = TimeHelper.GetVietnamTime().AddDays(2),
                    Status = GradingStatus.Assigned
                };
                await _unitOfWork.ExerciseGradingAssignments.CreateAsync(gradingAssignment);
            }

            lessonProgress.LastUpdated = TimeHelper.GetVietnamTime();
            await _unitOfWork.LessonProgresses.UpdateAsync(lessonProgress);

            await _unitOfWork.SaveChangesAsync();

            if (course?.GradingType == GradingType.AIOnly || course?.GradingType == GradingType.AIAndTeacher)
            {
                AssessmentRequest assessmentRequest = new AssessmentRequest
                {
                    ExerciseSubmissionId = submission.ExerciseSubmissionId,
                    Audio = request.Audio,
                    LanguageCode = course.Language.LanguageCode,
                    GradingType = course.GradingType.ToString()
                };

                await _exerciseGradingService.ProcessAIGradingAsync(assessmentRequest);
            }

            var response = new ExerciseSubmissionResponse
            {
                ExerciseSubmissionId = submission.ExerciseSubmissionId,
                ExerciseId = submission.ExerciseId,
                Status = submission.Status.ToString(),
                SubmittedAt = submission.SubmittedAt.ToString("dd-MM-yyyy")
            };

            return BaseResponse<ExerciseSubmissionResponse>.Success(response, "Exercise submitted successfully");
        }
        public async Task<BaseResponse<ProgressTrackingResponse>> TrackActivityAsync(Guid userId, TrackActivityRequest request)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == userId);
                if (learner == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Learner not found", 404);

                // Find lesson progress
                var lessonProgress = await _unitOfWork.LessonProgresses
                    .Query()
                    .Include(lp => lp.UnitProgress)
                    .ThenInclude(up => up.Enrollment)
                    .FirstOrDefaultAsync(lp => lp.LessonId == request.LessonId &&
                                             lp.UnitProgress.Enrollment.LearnerId == learner.LearnerLanguageId);

                if (lessonProgress == null)
                    return BaseResponse<ProgressTrackingResponse>.Fail(new object(), "Lesson progress not found", 404);

                // Log activity
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

                // Update lesson progress based on activity
                await UpdateLessonProgress(lessonProgress, request.LogType, request.DurationMinutes ?? 0);

                await _unitOfWork.SaveChangesAsync();

                return await BuildProgressResponse(lessonProgress.UnitProgress.EnrollmentId,
                    lessonProgress.UnitProgressId, lessonProgress.LessonProgressId);
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
                TeacherName = teacherAssignment?.Teacher?.FullName,
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
