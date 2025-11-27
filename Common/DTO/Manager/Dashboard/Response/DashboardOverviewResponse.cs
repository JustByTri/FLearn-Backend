namespace Common.DTO.Manager.Dashboard.Response
{
    public class DashboardOverviewResponse
    {
        public int TotalUsers { get; set; }
        public int NewRegistrations { get; set; } // Trong khoảng thời gian lọc
        public int ActiveLearners { get; set; }   // Dựa trên ActivityLog
        public decimal TotalRevenue { get; set; } // Doanh thu trong khoảng thời gian
        public double ChurnRate { get; set; }     // % user mua bài nhưng không học 30 ngày qua
        public List<ChartDataPoint> RevenueChart { get; set; } = new();
    }
    public class EngagementResponse
    {
        public double AvgTimeSpentPerUser { get; set; } // Phút
        public double AvgLessonCompletionRate { get; set; } // % chung toàn hệ thống
        public List<ChartDataPoint> ActivityVolumeChart { get; set; } = new();
    }

    // 3. DTO Hiệu quả nội dung (Content Effectiveness) - Tập trung vào Drop-off
    public class ContentEffectivenessResponse
    {
        public Guid LessonId { get; set; }
        public string CourseName { get; set; }
        public string LessonTitle { get; set; }
        public int TotalStarted { get; set; }    // Số người đã bắt đầu
        public int TotalCompleted { get; set; }  // Số người đã hoàn thành
        public double DropOffRate { get; set; }  // % người bỏ cuộc giữa chừng
        public double AvgTimeSpent { get; set; } // Thời gian trung bình ở bài này
    }

    // Class phụ hỗ trợ vẽ biểu đồ
    public class ChartDataPoint
    {
        public string Label { get; set; } // Ngày/Tháng
        public double Value { get; set; }
    }
}
