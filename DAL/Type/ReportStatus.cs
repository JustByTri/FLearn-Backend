namespace DAL.Type
{
    public enum ReportStatus
    {
        Pending = 0,        // Học viên vừa gửi
        UnderReview = 1,    // Đang được xem xét
        Resolved = 2,       // Đã xử lý xong
        Rejected = 3,       // Báo cáo không hợp lệ / bị từ chối
        Reopened = 4,       // Mở lại do chưa xử lý triệt để
        Closed = 5          // Đóng hoàn toàn, không chỉnh sửa nữa
    }
}
