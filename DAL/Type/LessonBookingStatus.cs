namespace DAL.Type
{
    public enum LessonBookingStatus
    {
        Pending = 0,            // Học viên gửi yêu cầu
        Confirmed = 1,          // Giáo viên xác nhận
        Rescheduled = 2,        // Đổi lịch
        CancelledByStudent = 3, // Học viên hủy
        CancelledByTeacher = 4, // Giáo viên hủy
        InProgress = 5,         // Đang diễn ra
        Completed = 6,          // Đã hoàn thành
        NoShowStudent = 7,      // Học viên vắng
        NoShowTeacher = 8,      // Giáo viên vắng
        Expired = 9             // Hết hạn (không diễn ra)
    }
}
