namespace DAL.Type
{
    public enum ReferenceType
    {
        CourseCreationFee,   // Phí tạo khóa học
        GradingFee,          // Phí chấm bài
        TeacherPayout,       // Thanh toán cho giáo viên
        WithdrawalRequest,   // Yêu cầu rút tiền của teacher
        Refund,              // Hoàn tiền
        CoursePurchase,      // Mua khóa học
        Class,               // Thanh toán từ lớp học (payout)
        ClassEnrollment      // Đăng ký lớp học (học viên thanh toán)
    }
}
