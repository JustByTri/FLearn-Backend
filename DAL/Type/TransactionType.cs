namespace DAL.Type
{
    public enum TransactionType
    {
        Deposit = 1,      // Nạp tiền vào ví (tăng balance)
        Withdrawal = 2,   // Rút tiền ra khỏi ví (giảm balance)
        Payout = 3,       // Hệ thống trả tiền cho teacher (tăng)
        Refund = 4,       // Hoàn tiền cho learner (tăng)
        Payment = 5,      // Thanh toán khóa học (giảm)
        Adjustment = 6,   // Điều chỉnh thủ công (admin thao tác)
        Transfer = 7,     // Chuyển tiền giữa ví
        Hold = 8,         // Tạm giữ số dư (ví dụ đang chờ xác nhận)
        Release = 9       // Giải phóng tiền tạm giữ
    }
}
