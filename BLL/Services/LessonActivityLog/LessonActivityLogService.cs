//using BLL.IServices.LessonActivityLog;
//using BLL.IServices.LessonProgress;
//using Common.DTO.ApiResponse;
//using Common.DTO.LessonLog.Request;
//using Common.DTO.LessonLog.Response;
//using DAL.Helpers;
//using DAL.Type;
//using DAL.UnitOfWork;
//using Microsoft.EntityFrameworkCore;

//namespace BLL.Services.LessonActivityLog
//{
//    public class LessonActivityLogService : ILessonActivityLogService
//    {
//        private readonly IUnitOfWork _unit;
//        private readonly ILessonProgressService _lessonProgressService;
//        public LessonActivityLogService(IUnitOfWork unit, ILessonProgressService lessonProgressService)
//        {
//            _unit = unit;
//            _lessonProgressService = lessonProgressService;
//        }
//        public async Task<BaseResponse<LessonLogResponse>> AddLogAsync(Guid userId, LessonLogRequest req)
//        {
//            try
//            {
//                var user = await _unit.Users.GetByIdAsync(userId);
//                var learner = await _unit.LearnerLanguages.FindAsync(l => l.UserId == userId);
//                if (user == null || learner == null)
//                    return BaseResponse<LessonLogResponse>.Fail(null, "Access denied.", 403);

//                var enrollment = await _unit.Enrollments.GetByIdAsync(req.EnrollmentId);
//                if (enrollment == null)
//                    return BaseResponse<LessonLogResponse>.Fail(null, "Enrollment not found.", 404);

//                if (enrollment.LearnerId != learner.LearnerLanguageId)
//                    return BaseResponse<LessonLogResponse>.Fail(null, "Access denied.", 403);

//                // 3️⃣ Kiểm tra lesson
//                var lesson = await _unit.Lessons.Query()
//                    .Include(l => l.CourseUnit)
//                    .ThenInclude(u => u.Course)
//                    .FirstOrDefaultAsync(l => l.LessonID == req.LessonId);

//                if (lesson == null)
//                    return BaseResponse<LessonLogResponse>.Fail(null, "Lesson not found.", 404);

//                if (lesson.CourseUnit == null || lesson.CourseUnit.CourseID != enrollment.CourseId)
//                    return BaseResponse<LessonLogResponse>.Fail(null, "Lesson not in this course.", 403);

//                var log = new DAL.Models.LessonActivityLog
//                {
//                    LessonActivityLogId = Guid.NewGuid(),
//                    LessonId = req.LessonId,
//                    EnrollmentId = req.EnrollmentId,
//                    LearnerId = learner.LearnerLanguageId,
//                    ActivityType = (LessonLogType)req.Type,
//                    Value = req.Value,
//                    MetadataJson = req.MetadataJson,
//                    CreatedAt = TimeHelper.GetVietnamTime()
//                };

//                await _unit.LessonActivityLogs.CreateAsync(log);
//                await _unit.SaveChangesAsync();

//                double lessonProgress = await CalculateLessonProgressFromLogsAsync(userId, req.EnrollmentId, req.LessonId);

//                await _lessonProgressService.UpdateLessonProgressAsync(userId, req.EnrollmentId, req.LessonId, lessonProgress);

//                var response = new LessonLogResponse
//                {
//                    LessonActivityLogId = log.LessonActivityLogId,
//                    LessonId = log.LessonId,
//                    EnrollmentId = log.EnrollmentId,
//                    LearnerId = log.LearnerId,
//                    ActivityType = log.ActivityType.ToString(),
//                    Value = log.Value,
//                    MetadataJson = log.MetadataJson,
//                    CreatedAt = log.CreatedAt
//                };

//                return BaseResponse<LessonLogResponse>.Success(response, "Log recorded and progress updated.");
//            }
//            catch (Exception ex)
//            {
//                return BaseResponse<LessonLogResponse>.Error($"Unexpected error: {ex.Message}");
//            }
//        }
//        public async Task<double> CalculateLessonProgressFromLogsAsync(Guid userId, Guid enrollmentId, Guid lessonId)
//        {
//            var user = await _unit.Users.GetByIdAsync(userId);
//            var learner = await _unit.LearnerLanguages.FindAsync(l => l.UserId == userId);
//            if (user == null || learner == null)
//                throw new InvalidOperationException("Access denied.");

//            var lesson = await _unit.Lessons.Query()
//                .Include(l => l.Exercises)
//                .FirstOrDefaultAsync(l => l.LessonID == lessonId);

//            if (lesson == null)
//                throw new InvalidOperationException("Lesson not found.");

//            var logs = await _unit.LessonActivityLogs.Query()
//                .Where(l => l.LearnerId == learner.LearnerLanguageId && l.LessonId == lessonId && l.EnrollmentId == enrollmentId)
//                .ToListAsync();

//            int partCount = 0;
//            double total = 0;

//            if (!string.IsNullOrEmpty(lesson.Content))
//            {
//                partCount++;
//                if (logs.Any(l => l.ActivityType == LessonLogType.ContentRead))
//                    total += 1;
//            }

//            if (!string.IsNullOrEmpty(lesson.VideoUrl))
//            {
//                partCount++;
//                double videoPercent = logs
//                    .Where(l => l.ActivityType == LessonLogType.VideoProgress)
//                    .OrderByDescending(l => l.CreatedAt)
//                    .Select(l => l.Value ?? 0)
//                    .FirstOrDefault();
//                total += (videoPercent / 100);
//            }

//            if (!string.IsNullOrEmpty(lesson.DocumentUrl))
//            {
//                partCount++;
//                if (logs.Any(l => l.ActivityType == LessonLogType.PdfOpened))
//                    total += 1;
//            }

//            if (lesson.Exercises.Any())
//            {
//                partCount++;
//                var totalExercises = lesson.Exercises.Count;
//                var passedCount = await _unit.ExerciseSubmissions.Query()
//                    .Include(s => s.Exercise)
//                    .CountAsync(s => s.LearnerId == learner.LearnerLanguageId
//                                     && s.Exercise.LessonID == lessonId
//                                     && s.IsPassed);

//                total += (double)passedCount / totalExercises;
//            }

//            if (partCount == 0) return 100;

//            return Math.Min((total / partCount) * 100, 100);
//        }
//    }
//}
