using Common.DTO.ApiResponse;
using Common.DTO.Manager.Dashboard.Response;

namespace BLL.IServices.Dashboard
{
    public interface IManagerDashboardService
    {
        Task<BaseResponse<DashboardOverviewResponse>> GetKpiOverviewAsync(DateTime startDate, DateTime endDate);

        // Lấy dữ liệu về thời lượng học và mức độ tương tác
        Task<BaseResponse<EngagementResponse>> GetEngagementMetricsAsync(DateTime startDate, DateTime endDate);

        // Phân tích xu hướng học tập và tìm ra các bài học có tỷ lệ bỏ cuộc cao (Pain points)
        Task<BaseResponse<List<ContentEffectivenessResponse>>> GetContentEffectivenessAsync(int topRecords = 10);
    }
}
