namespace DAL.Type
{
    public enum EarningStatus
    {
        Pending = 1,       // Chờ duyệt (vừa tạo)
        Approved = 2,      // Được duyệt, sẽ trả tiền
        Rejected = 3,      // Không được trả
        RegradingRequested = 4, // Yêu cầu chấm lại
    }
}
