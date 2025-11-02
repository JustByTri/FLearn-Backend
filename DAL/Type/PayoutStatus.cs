namespace DAL.Type
{
    public enum PayoutStatus
    {
        Pending = 1,      // Giáo viên gửi yêu cầu, chờ admin xử lý
        Processing = 2,   // Admin đã duyệt, đang chuyển tiền (qua PayOS, bank, v.v.)
        Completed = 3,    // Rút tiền thành công
        Failed = 4,       // Lỗi giao dịch (ngân hàng, gateway, v.v.)
        Cancelled = 5,    // Giáo viên hủy hoặc admin từ chối
        Rejected = 6      // Admin từ chối yêu cầu (lý do rõ ràng)
    }
}
