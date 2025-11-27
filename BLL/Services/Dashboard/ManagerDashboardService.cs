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
        public async Task<BaseResponse<DashboardOverviewResponse>> GetKpiOverviewAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Chuẩn hóa thời gian: Start đầu ngày, End cuối ngày
                var start = startDate.Date;
                var end = endDate.Date.AddDays(1).AddTicks(-1);
                var now = TimeHelper.GetVietnamTime();
                var thirtyDaysAgo = now.AddDays(-30);

                // 1. New Registrations & Total Users
                var totalUsers = await _unitOfWork.Users.Query().CountAsync();
                var newRegistrations = await _unitOfWork.Users.Query()
                    .CountAsync(u => u.CreatedAt >= start && u.CreatedAt <= end);

                // 2. Active Learners (Theo yêu cầu 1: Dùng LessonActivityLog)
                // Đếm số LearnerId duy nhất có log hoạt động trong khoảng thời gian lọc
                var activeLearners = await _unitOfWork.LessonActivityLogs.Query()
                    .Where(log => log.CreatedAt >= start && log.CreatedAt <= end)
                    .Select(log => log.LearnerId)
                    .Distinct()
                    .CountAsync();

                // 3. Revenue
                var revenueQuery = _unitOfWork.Purchases.Query()
                    .Where(p => p.Status == PurchaseStatus.Completed &&
                                p.CreatedAt >= start && p.CreatedAt <= end);

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

                // 4. Churn Rate (Theo yêu cầu 2: Có mua khóa học nhưng ko học trong 30 ngày)
                // Bước 4.1: Lấy danh sách user đã từng mua ít nhất 1 khóa học (Paid Users)
                var paidUserIds = await _unitOfWork.Purchases.Query()
                    .Where(p => p.Status == PurchaseStatus.Completed)
                    .Select(p => p.UserId)
                    .Distinct()
                    .ToListAsync(); // Lấy về memory để xử lý nếu DB không quá lớn, hoặc dùng Queryable join

                double churnRate = 0;
                if (paidUserIds.Any())
                {
                    // Bước 4.2: Trong số Paid Users, tìm những người CÓ hoạt động trong 30 ngày qua
                    // Cần join từ LearnerLanguages để map UserId -> LearnerId -> ActivityLog
                    var activePaidUsersCount = await _unitOfWork.LessonActivityLogs.Query()
                        .Include(log => log.Learner)
                        .Where(log => log.CreatedAt >= thirtyDaysAgo &&
                                      paidUserIds.Contains(log.Learner.UserId))
                        .Select(log => log.Learner.UserId)
                        .Distinct()
                        .CountAsync();

                    // Bước 4.3: Tính Churn
                    // Churn = (Tổng User trả phí - User trả phí còn active) / Tổng User trả phí
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

        public async Task<BaseResponse<EngagementResponse>> GetEngagementMetricsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var start = startDate.Date;
                var end = endDate.Date.AddDays(1).AddTicks(-1);

                // 1. Tính tổng thời gian học (Average Time Spent)
                var logsInRange = _unitOfWork.LessonActivityLogs.Query()
                    .Where(l => l.CreatedAt >= start && l.CreatedAt <= end);

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

                // 3. Tỷ lệ hoàn thành trung bình (Completion Rate) trong giai đoạn này
                // Đếm số lesson được set status = Completed trong khoảng thời gian
                var completedCount = await _unitOfWork.LessonProgresses.Query()
                    .CountAsync(lp => lp.Status == LearningStatus.Completed &&
                                      lp.CompletedAt >= start && lp.CompletedAt <= end);

                var startedCount = await _unitOfWork.LessonProgresses.Query()
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

        public async Task<BaseResponse<List<ContentEffectivenessResponse>>> GetContentEffectivenessAsync(int topRecords = 10)
        {
            try
            {
                // Theo yêu cầu 3: Tìm Lesson có nhiều người bắt đầu nhưng ít người hoàn thành (Drop-off cao)

                // Lấy tất cả LessonProgress để tính toán
                // Lưu ý: Query này có thể nặng nếu dữ liệu lớn -> Nên cache hoặc chạy background job định kỳ cập nhật bảng thống kê riêng
                var lessonStats = await _unitOfWork.LessonProgresses.Query()
                    .GroupBy(lp => lp.LessonId)
                    .Select(g => new
                    {
                        LessonId = g.Key,
                        TotalStarted = g.Count(), // Tất cả record trong LessonProgress đều là đã Start
                        TotalCompleted = g.Count(lp => lp.Status == LearningStatus.Completed)
                    })
                    .Where(x => x.TotalStarted > 5) // Chỉ xét các bài học có ít nhất 5 người học để tránh nhiễu
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
                        // Drop-off = (Started - Completed) / Started
                        dropOffRate = (double)(stat.TotalStarted - stat.TotalCompleted) / stat.TotalStarted * 100;
                    }

                    // Tùy chọn: Lấy thời gian trung bình (nặng, có thể bỏ qua nếu cần tốc độ)
                    // double avgTime = await _unitOfWork.LessonActivityLogs.Query()
                    //     .Where(l => l.LessonId == stat.LessonId)
                    //     .AverageAsync(l => l.Value ?? 0);

                    report.Add(new ContentEffectivenessResponse
                    {
                        LessonId = stat.LessonId,
                        LessonTitle = lessonDetails[stat.LessonId].Title,
                        CourseName = lessonDetails[stat.LessonId].CourseTitle,
                        TotalStarted = stat.TotalStarted,
                        TotalCompleted = stat.TotalCompleted,
                        DropOffRate = Math.Round(dropOffRate, 2),
                        AvgTimeSpent = 0 // Tạm để 0 để tối ưu performance
                    });
                }

                // Sắp xếp theo tỷ lệ Drop-off giảm dần (Bài nào drop nhiều nhất lên đầu)
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
    }
}
