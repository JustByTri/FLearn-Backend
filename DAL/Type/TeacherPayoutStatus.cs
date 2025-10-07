namespace DAL.Type
{
    public enum TeacherPayoutStatus
    {
        Pending = 0,       // Chờ duyệt
        UnderReview = 1,   // Đang kiểm tra
        Approved = 2,      // Đã duyệt
        Processing = 3,    // Đang xử lý thanh toán
        Paid = 4,          // Đã trả tiền
        Failed = 5,        // Thanh toán thất bại
        Cancelled = 6      // Bị hủy
    }
}
