using BLL.IServices.LessonProgress;
using Common.DTO.ApiResponse;
using Common.DTO.LessonProgress.Response;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Services.LessonProgress
{
    public class LessonProgressService : ILessonProgressService
    {
        private readonly IUnitOfWork _unit;
        private readonly ILogger<LessonProgressService> _logger;
        public LessonProgressService(IUnitOfWork unit, ILogger<LessonProgressService> logger)
        {
            _unit = unit;
            _logger = logger;
        }
        public async Task<BaseResponse<LearnerProgressResponse>> CompleteLessonAsync(Guid userId, Guid enrollmentId, Guid lessonId, bool forceComplete = false)
        {
            try
            {
                var user = await _unit.Users.GetByIdAsync(userId);
                var learner = await _unit.LearnerLanguages.FindAsync(l => l.UserId == userId);

                if (user == null || learner == null)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Access denied.", 403);

                var enrollment = await _unit.Enrollments.GetByIdAsync(enrollmentId);
                if (enrollment == null)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Resource not found.", 404);

                if (enrollment.LearnerId != learner.LearnerLanguageId)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Access denied.", 403);

                var lesson = await _unit.Lessons.Query()
                    .Include(l => l.CourseUnit)
                    .OrderBy(l => l.CreatedAt)
                    .Where(l => l.LessonID == lessonId)
                    .FirstOrDefaultAsync();
                if (lesson == null)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Resource not found.", 404);

                if (lesson.CourseUnit == null || lesson.CourseUnit.CourseID != enrollment.CourseId)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Access denied.", 403);

                var progress = await _unit.LearnerProgresses.FindAsync(p => p.EnrollmentId == enrollmentId && p.LessonId == lessonId);
                if (progress == null)
                {
                    progress = new LearnerProgress
                    {
                        LearnerProgressId = Guid.NewGuid(),
                        EnrollmentId = enrollmentId,
                        LessonId = lessonId,
                        StartedAt = DateTime.UtcNow,
                        ProgressPercent = 100,
                        IsCompleted = true,
                        CompletedAt = TimeHelper.GetVietnamTime(),
                    };
                    _unit.LearnerProgresses.Create(progress);
                }
                else
                {
                    if (!progress.IsCompleted)
                    {
                        progress.IsCompleted = true;
                        progress.ProgressPercent = 100;
                        progress.CompletedAt = TimeHelper.GetVietnamTime();
                        _unit.LearnerProgresses.Update(progress);
                    }
                }

                await _unit.SaveChangesAsync();

                var enrollmentPercent = await RecalculateEnrollmentProgressAsync(enrollmentId);
                if (Math.Abs(enrollmentPercent - 100.0) < 0.0001 && enrollment.Status != DAL.Models.EnrollmentStatus.Completed)
                {
                    enrollment.Status = DAL.Models.EnrollmentStatus.Completed;
                    enrollment.CompletedAt = DateTime.UtcNow;
                    _unit.Enrollments.Update(enrollment);
                    await _unit.SaveChangesAsync();
                }

                return BaseResponse<LearnerProgressResponse>.Success(MapToResponse(progress));
            }
            catch (Exception ex)
            {
                return BaseResponse<LearnerProgressResponse>.Fail(null, $"Unexpected error occurred: {ex.Message}.", 500);
            }
        }

        public async Task<BaseResponse<LearnerProgressResponse>> GetLessonProgressAsync(Guid userId, Guid enrollmentId, Guid lessonId)
        {
            try
            {
                var user = await _unit.Users.GetByIdAsync(userId);
                var learner = await _unit.LearnerLanguages.FindAsync(l => l.UserId == userId);

                if (user == null || learner == null)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Access denied.", 403);

                var enrollment = await _unit.Enrollments.GetByIdAsync(enrollmentId);
                if (enrollment == null)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Resource not found.", 404);

                if (enrollment.LearnerId != learner.LearnerLanguageId)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Access denied.", 403);

                var lesson = await _unit.Lessons.Query()
                    .Include(l => l.CourseUnit)
                    .OrderBy(l => l.CreatedAt)
                    .Where(l => l.LessonID == lessonId)
                    .FirstOrDefaultAsync();
                if (lesson == null)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Resource not found.", 404);

                if (lesson.CourseUnit == null || lesson.CourseUnit.CourseID != enrollment.CourseId)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Access denied.", 403);

                var progress = await _unit.LearnerProgresses.FindAsync(p => p.EnrollmentId == enrollmentId && p.LessonId == lessonId);
                if (progress == null)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Resource not found.", 404);

                return BaseResponse<LearnerProgressResponse>.Success(MapToResponse(progress));
            }
            catch (Exception ex)
            {
                return BaseResponse<LearnerProgressResponse>.Fail(null, $"Unexpected error occurred: {ex.Message}.", 500);
            }
        }

        public async Task<BaseResponse<IEnumerable<LearnerProgressResponse>>> GetProgressesByEnrollmentAsync(Guid userId, Guid enrollmentId)
        {
            try
            {
                var user = await _unit.Users.GetByIdAsync(userId);
                var learner = await _unit.LearnerLanguages.FindAsync(l => l.UserId == userId);

                if (user == null || learner == null)
                    return BaseResponse<IEnumerable<LearnerProgressResponse>>.Fail(null, "Access denied.", 403);

                var enrollment = await _unit.Enrollments.GetByIdAsync(enrollmentId);
                if (enrollment == null)
                    return BaseResponse<IEnumerable<LearnerProgressResponse>>.Fail(null, "Resource not found.", 404);

                if (enrollment.LearnerId != learner.LearnerLanguageId)
                    return BaseResponse<IEnumerable<LearnerProgressResponse>>.Fail(null, "Access denied.", 403);

                var progresses = await _unit.LearnerProgresses.FindAllAsync(p => p.EnrollmentId == enrollmentId);

                return BaseResponse<IEnumerable<LearnerProgressResponse>>.Success(progresses.Select(MapToResponse).ToList());
            }
            catch (Exception ex)
            {
                return BaseResponse<IEnumerable<LearnerProgressResponse>>.Fail(null, $"Unexpected error occurred: {ex.Message}.", 500);
            }
        }

        public async Task<BaseResponse<LearnerProgressResponse>> StartLessonAsync(Guid userId, Guid enrollmentId, Guid lessonId)
        {
            try
            {
                var user = await _unit.Users.GetByIdAsync(userId);
                var learner = await _unit.LearnerLanguages.FindAsync(l => l.UserId == userId);

                if (user == null || learner == null)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Access denied.", 403);

                var enrollment = await _unit.Enrollments.GetByIdAsync(enrollmentId);
                if (enrollment == null)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Resource not found.", 404);

                if (enrollment.LearnerId != learner.LearnerLanguageId)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Access denied.", 403);

                var lesson = await _unit.Lessons.Query()
                    .Include(l => l.CourseUnit)
                    .OrderBy(l => l.CreatedAt)
                    .Where(l => l.LessonID == lessonId)
                    .FirstOrDefaultAsync();
                if (lesson == null)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Resource not found.", 404);

                if (lesson.CourseUnit == null || lesson.CourseUnit.CourseID != enrollment.CourseId)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Access denied.", 403);

                var progress = await _unit.LearnerProgresses.FindAsync(p => p.EnrollmentId == enrollmentId && p.LessonId == lessonId);

                if (progress == null)
                {
                    progress = new LearnerProgress
                    {
                        LearnerProgressId = Guid.NewGuid(),
                        EnrollmentId = enrollmentId,
                        LessonId = lessonId,
                        IsCompleted = false,
                        ProgressPercent = 0,
                        StartedAt = TimeHelper.GetVietnamTime(),
                    };
                    _unit.LearnerProgresses.Create(progress);
                }
                else
                {
                    if (!progress.StartedAt.HasValue)
                        progress.StartedAt = TimeHelper.GetVietnamTime();

                    _unit.LearnerProgresses.Update(progress);
                }

                await _unit.SaveChangesAsync();
                return BaseResponse<LearnerProgressResponse>.Success(MapToResponse(progress));
            }
            catch
            {
                return BaseResponse<LearnerProgressResponse>.Fail(null, "Unexpected error occurred.", 500);
            }
        }

        public async Task<BaseResponse<LearnerProgressResponse>> UpdateLessonProgressAsync(Guid userId, Guid enrollmentId, Guid lessonId, double progressPercent)
        {
            try
            {
                var user = await _unit.Users.GetByIdAsync(userId);
                var learner = await _unit.LearnerLanguages.FindAsync(l => l.UserId == userId);

                if (user == null || learner == null)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Access denied.", 403);

                var enrollment = await _unit.Enrollments.GetByIdAsync(enrollmentId);
                if (enrollment == null)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Resource not found.", 404);

                if (enrollment.LearnerId != learner.LearnerLanguageId)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Access denied.", 403);

                var lesson = await _unit.Lessons.Query()
                    .Include(l => l.CourseUnit)
                    .OrderBy(l => l.CreatedAt)
                    .Where(l => l.LessonID == lessonId)
                    .FirstOrDefaultAsync();
                if (lesson == null)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Resource not found.", 404);

                if (lesson.CourseUnit == null || lesson.CourseUnit.CourseID != enrollment.CourseId)
                    return BaseResponse<LearnerProgressResponse>.Fail(null, "Access denied.", 403);

                var progress = await _unit.LearnerProgresses.FindAsync(p => p.EnrollmentId == enrollmentId && p.LessonId == lessonId);
                if (progress == null)
                {
                    progress = new LearnerProgress
                    {
                        LearnerProgressId = Guid.NewGuid(),
                        EnrollmentId = enrollmentId,
                        LessonId = lessonId,
                        StartedAt = DateTime.UtcNow,
                        ProgressPercent = progressPercent,
                        IsCompleted = progressPercent >= 100,
                        CompletedAt = progressPercent >= 100 ? TimeHelper.GetVietnamTime() : (DateTime?)null
                    };
                    _unit.LearnerProgresses.Create(progress);
                }
                else
                {
                    progress.ProgressPercent = Math.Max(progress.ProgressPercent, progressPercent);

                    if (progress.ProgressPercent >= 100)
                    {
                        progress.IsCompleted = true;
                        if (!progress.CompletedAt.HasValue) progress.CompletedAt = TimeHelper.GetVietnamTime();
                    }
                    _unit.LearnerProgresses.Update(progress);
                }

                await _unit.SaveChangesAsync();
                await RecalculateEnrollmentProgressAsync(enrollmentId);

                return BaseResponse<LearnerProgressResponse>.Success(MapToResponse(progress));
            }
            catch (Exception ex)
            {
                return BaseResponse<LearnerProgressResponse>.Fail(null, $"Unexpected error occurred: {ex.Message}.", 500);
            }
        }

        public async Task<double> RecalculateEnrollmentProgressAsync(Guid enrollmentId)
        {
            var enrollment = await _unit.Enrollments.Query()
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);
            if (enrollment == null) throw new InvalidOperationException("Enrollment not found.");

            // get total lessons for the course
            var course = await _unit.Courses.GetByIdAsync(enrollment.CourseId);
            var totalLessons = course.NumLessons;

            if (totalLessons == 0)
            {
                enrollment.ProgressPercent = 0;
                _unit.Enrollments.Update(enrollment);
                await _unit.SaveChangesAsync();
                return 0;
            }

            // get progress per lesson (if missing, treated as 0)
            var progresses = await _unit.LearnerProgresses.Query()
                .Where(p => p.EnrollmentId == enrollmentId)
                .ToListAsync();

            // sum progressPercent for lessons that exist, missing lessons implicitly 0
            double sumProgress = progresses.Sum(p => p.ProgressPercent);
            double overallPercent = (sumProgress / totalLessons);

            // but if progresses list does not include all lessons, missing ones contribute 0 as intended
            // Clamp
            if (overallPercent < 0) overallPercent = 0;
            if (overallPercent > 100) overallPercent = 100;

            // store in enrollment
            enrollment.ProgressPercent = overallPercent;
            _unit.Enrollments.Update(enrollment);
            await _unit.SaveChangesAsync();

            return overallPercent;
        }
        private static LearnerProgressResponse MapToResponse(LearnerProgress p)
        {
            return new LearnerProgressResponse
            {
                LearnerProgressId = p.LearnerProgressId,
                EnrollmentId = p.EnrollmentId,
                LessonId = p.LessonId,
                IsCompleted = p.IsCompleted,
                ProgressPercent = p.ProgressPercent,
                StartedAt = p.StartedAt,
                CompletedAt = p.CompletedAt
            };
        }
    }
}
