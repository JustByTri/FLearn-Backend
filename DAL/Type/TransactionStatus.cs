namespace DAL.Type
{
    public enum TransactionStatus
    {
        Pending = 0,       // Khởi tạo, chờ xử lý
        Processing = 1,    // Đang xử lý (gửi đến cổng thanh toán)
        Succeeded = 2,     // Thành công
        Failed = 3,        // Thất bại
        Cancelled = 4,     // Bị hủy
        Refunded = 5,      // Đã hoàn tiền
        Expired = 6        // Hết hạn chưa xử lý
    }
}
