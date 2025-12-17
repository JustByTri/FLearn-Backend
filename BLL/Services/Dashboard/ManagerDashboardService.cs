using BLL.IServices.Dashboard;
using Common.DTO.ApiResponse;
using Common.DTO.Manager.Dashboard.Response;
using DAL.Helpers;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Dashboard
{
    public class ManagerDashboardService : IManagerDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ManagerDashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<BaseResponse<DashboardOverviewResponse>> GetKpiOverviewAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var targetLanguageId = await GetManagerLanguageIdAsync(userId);
                // Chuẩn hóa thời gian: Start đầu ngày, End cuối ngày
                var start = startDate.Date;
                var end = endDate.Date.AddDays(1).AddTicks(-1);
                var now = TimeHelper.GetVietnamTime();
                var thirtyDaysAgo = now.AddDays(-30);

                // 1. New Registrations & Total Users (Lọc theo Language)
                // Total User: Đếm số lượng User ĐANG HỌC ngôn ngữ này (có record trong LearnerLanguage)
                var totalUsers = await _unitOfWork.LearnerLanguages.Query()
                    .Where(ll => ll.LanguageId == targetLanguageId)
                    .Select(ll => ll.UserId) // Distinct User vì 1 user có thể có nhiều level
                    .Distinct()
                    .CountAsync();

                // New Registration: Đếm User mới đăng ký học ngôn ngữ này trong khoảng thời gian
                var newRegistrations = await _unitOfWork.LearnerLanguages.Query()
                    .Where(ll => ll.LanguageId == targetLanguageId &&
                                 ll.CreatedAt >= start && ll.CreatedAt <= end)
                    .Select(ll => ll.UserId)
                    .Distinct()
                    .CountAsync();

                // 2. Active Learners
                // Logic: Learner có log activity -> Log thuộc Lesson -> Lesson thuộc Course -> Course thuộc Language
                var activeLearners = await _unitOfWork.LessonActivityLogs.Query()
                    .Include(log => log.Lesson)
                        .ThenInclude(l => l.CourseUnit)
                        .ThenInclude(cu => cu.Course)
                    .Where(log => log.CreatedAt >= start && log.CreatedAt <= end &&
                                  log.Lesson.CourseUnit.Course.LanguageId == targetLanguageId) // Filter Language
                    .Select(log => log.LearnerId)
                    .Distinct()
                    .CountAsync();

                // 3. Revenue (Lọc theo khóa học thuộc ngôn ngữ đó)
                var revenueQuery = _unitOfWork.Purchases.Query()
                    .Include(p => p.Course)
                    .Where(p => p.Status == PurchaseStatus.Completed &&
                                p.CreatedAt >= start && p.CreatedAt <= end &&
                                p.Course.LanguageId == targetLanguageId); // Filter Language

                var totalRevenue = await revenueQuery.SumAsync(p => p.FinalAmount);

                var rawRevenueData = await revenueQuery
                                    .GroupBy(p => p.CreatedAt.Date)
                                    .Select(g => new
                                    {
                                        Date = g.Key,
                                        TotalAmount = g.Sum(p => p.FinalAmount)
                                    })
                                    .OrderBy(x => x.Date)
                                    .ToListAsync();

                var revenueChart = rawRevenueData
                                    .Select(x => new ChartDataPoint
                                    {
                                        Label = x.Date.ToString("dd/MM"),
                                        Value = (double)x.TotalAmount
                                    })
                                    .ToList();

                // 4. Churn Rate (Trong phạm vi Language đó)
                // 4.1: Lấy Paid Users cho Language này
                var paidUserIds = await _unitOfWork.Purchases.Query()
                    .Include(p => p.Course)
                    .Where(p => p.Status == PurchaseStatus.Completed &&
                                p.Course.LanguageId == targetLanguageId)
                    .Select(p => p.UserId)
                    .Distinct()
                    .ToListAsync();

                double churnRate = 0;
                if (paidUserIds.Any())
                {
                    // 4.2: Paid Users CÓ hoạt động (trong Language này) trong 30 ngày qua
                    var activePaidUsersCount = await _unitOfWork.LessonActivityLogs.Query()
                        .Include(log => log.Learner)
                        .Include(log => log.Lesson)
                            .ThenInclude(l => l.CourseUnit)
                            .ThenInclude(cu => cu.Course)
                        .Where(log => log.CreatedAt >= thirtyDaysAgo &&
                                      paidUserIds.Contains(log.Learner.UserId) &&
                                      log.Lesson.CourseUnit.Course.LanguageId == targetLanguageId) // Quan trọng: Chỉ tính hoạt động ở ngôn ngữ này
                        .Select(log => log.Learner.UserId)
                        .Distinct()
                        .CountAsync();

                    // 4.3: Tính Churn
                    int churnedUsers = paidUserIds.Count - activePaidUsersCount;
                    churnRate = ((double)churnedUsers / paidUserIds.Count) * 100;
                }

                return BaseResponse<DashboardOverviewResponse>.Success(new DashboardOverviewResponse
                {
                    TotalUsers = totalUsers,
                    NewRegistrations = newRegistrations,
                    ActiveLearners = activeLearners,
                    TotalRevenue = totalRevenue,
                    RevenueChart = revenueChart,
                    ChurnRate = Math.Round(churnRate, 2)
                }, "KPI overview retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<DashboardOverviewResponse>.Error($"Error calculating KPI: {ex.Message}");
            }
        }

        public async Task<BaseResponse<EngagementResponse>> GetEngagementMetricsAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var targetLanguageId = await GetManagerLanguageIdAsync(userId);
                var start = startDate.Date;
                var end = endDate.Date.AddDays(1).AddTicks(-1);

                var logsInRange = _unitOfWork.LessonActivityLogs.Query()
                                    .Include(l => l.Lesson)
                                        .ThenInclude(l => l.CourseUnit)
                                        .ThenInclude(cu => cu.Course)
                                    .Where(l => l.CreatedAt >= start && l.CreatedAt <= end &&
                                                l.Lesson.CourseUnit.Course.LanguageId == targetLanguageId);

                var totalMinutes = await logsInRange.SumAsync(l => l.Value ?? 0);
                var distinctLearners = await logsInRange.Select(l => l.LearnerId).Distinct().CountAsync();

                double avgTime = distinctLearners > 0 ? totalMinutes / distinctLearners : 0;

                var rawActivityData = await logsInRange
                    .GroupBy(l => l.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date) // Sắp xếp theo ngày
                    .ToListAsync();

                // 2. Biểu đồ Volume hoạt động
                var activityChart = rawActivityData
                    .Select(x => new ChartDataPoint
                    {
                        Label = x.Date.ToString("dd/MM"),
                        Value = x.Count
                    })
                    .ToList();

                // 2. Tỷ lệ hoàn thành trung bình (Lọc theo Lesson của Language này)
                var lessonProgressQuery = _unitOfWork.LessonProgresses.Query()
                    .Include(lp => lp.Lesson)
                        .ThenInclude(l => l.CourseUnit)
                        .ThenInclude(cu => cu.Course)
                    .Where(lp => lp.Lesson.CourseUnit.Course.LanguageId == targetLanguageId);

                var completedCount = await lessonProgressQuery
                                    .CountAsync(lp => lp.Status == LearningStatus.Completed &&
                                                      lp.CompletedAt >= start && lp.CompletedAt <= end);

                var startedCount = await lessonProgressQuery
                    .CountAsync(lp => lp.StartedAt >= start && lp.StartedAt <= end);

                double completionRate = startedCount > 0
                    ? ((double)completedCount / startedCount) * 100
                    : 0;

                return BaseResponse<EngagementResponse>.Success(new EngagementResponse
                {
                    AvgTimeSpentPerUser = Math.Round(avgTime, 2),
                    AvgLessonCompletionRate = Math.Round(completionRate, 2),
                    ActivityVolumeChart = activityChart
                }, "Engagement metrics retrieved");
            }
            catch (Exception ex)
            {
                return BaseResponse<EngagementResponse>.Error($"Error calculating engagement: {ex.Message}");
            }
        }

        public async Task<BaseResponse<List<ContentEffectivenessResponse>>> GetContentEffectivenessAsync(Guid userId, int topRecords = 10)
        {
            try
            {
                var targetLanguageId = await GetManagerLanguageIdAsync(userId);

                var lessonStats = await _unitOfWork.LessonProgresses.Query()
                                    .Include(lp => lp.Lesson)
                                        .ThenInclude(l => l.CourseUnit)
                                        .ThenInclude(cu => cu.Course)
                                    .Where(lp => lp.Lesson.CourseUnit.Course.LanguageId == targetLanguageId) // Filter Language
                                    .GroupBy(lp => lp.LessonId)
                                    .Select(g => new
                                    {
                                        LessonId = g.Key,
                                        TotalStarted = g.Count(),
                                        TotalCompleted = g.Count(lp => lp.Status == LearningStatus.Completed)
                                    })
                                    .Where(x => x.TotalStarted > 5)
                                    .ToListAsync();

                // Tính Drop-off rate in-memory
                var lessonIds = lessonStats.Select(x => x.LessonId).ToList();

                // Lấy thông tin chi tiết bài học (Tên, Course)
                var lessonDetails = await _unitOfWork.Lessons.Query()
                    .Include(l => l.CourseUnit).ThenInclude(u => u.Course)
                    .Where(l => lessonIds.Contains(l.LessonID))
                    .ToDictionaryAsync(l => l.LessonID, l => new { l.Title, CourseTitle = l.CourseUnit.Course.Title });

                var report = new List<ContentEffectivenessResponse>();

                foreach (var stat in lessonStats)
                {
                    if (!lessonDetails.ContainsKey(stat.LessonId)) continue;

                    double dropOffRate = 0;
                    if (stat.TotalStarted > 0)
                    {
                        dropOffRate = (double)(stat.TotalStarted - stat.TotalCompleted) / stat.TotalStarted * 100;
                    }


                    double avgTime = await _unitOfWork.LessonActivityLogs.Query()
                         .Where(l => l.LessonId == stat.LessonId)
                         .AverageAsync(l => l.Value ?? 0);

                    report.Add(new ContentEffectivenessResponse
                    {
                        LessonId = stat.LessonId,
                        LessonTitle = lessonDetails[stat.LessonId].Title,
                        CourseName = lessonDetails[stat.LessonId].CourseTitle,
                        TotalStarted = stat.TotalStarted,
                        TotalCompleted = stat.TotalCompleted,
                        DropOffRate = Math.Round(dropOffRate, 2),
                        AvgTimeSpent = avgTime
                    });
                }

                var result = report
                    .OrderByDescending(x => x.DropOffRate)
                    .ThenByDescending(x => x.TotalStarted)
                    .Take(topRecords)
                    .ToList();

                return BaseResponse<List<ContentEffectivenessResponse>>.Success(result, "Content effectiveness analysis retrieved");
            }
            catch (Exception ex)
            {
                return BaseResponse<List<ContentEffectivenessResponse>>.Error($"Error analyzing content: {ex.Message}");
            }
        }
        #region
        private async Task<Guid> GetManagerLanguageIdAsync(Guid managerId)
        {
            var managerLang = await _unitOfWork.ManagerLanguages.FindAsync(ml => ml.UserId == managerId && ml.Status == true);

            if (managerLang == null)
            {
                throw new Exception("Manager is not assigned to any language.");
            }

            return managerLang.LanguageId;
        }
        #endregion
    }
}
