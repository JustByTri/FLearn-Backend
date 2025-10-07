namespace DAL.Type
{
    public enum DisputeStatus
    {
        Submitted = 0,      // Giáo viên gửi khiếu nại
        UnderReview = 1,    // Staff đang xem xét
        NeedMoreInfo = 2,   // Cần thêm thông tin
        Approved = 3,       // Chấp nhận khiếu nại
        Rejected = 4,       // Bác bỏ khiếu nại
        Withdrawn = 5,      // Giáo viên rút lại
        Closed = 6          // Đã đóng hoàn toàn
    }
}
