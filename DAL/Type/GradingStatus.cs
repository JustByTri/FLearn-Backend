namespace DAL.Type
{
    public enum GradingStatus
    {
        Pending = 1,      // Mới tạo, chưa gán giáo viên
        Assigned = 2,     // Đã gán, đang chờ chấm
        InReview = 3,     // Giáo viên đang chấm
        Returned = 4,     // Đã chấm xong, có kết quả
        Reopened = 5,     // Mở lại để chấm lại
        Expired = 6,      // Quá deadline mà chưa chấm
        Cancelled = 7,    // Bị hủy
        Revoked = 8       // Kết quả bị thu hồi
    }
}
