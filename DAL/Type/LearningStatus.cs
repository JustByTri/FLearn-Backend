namespace DAL.Type
{
    public enum LearningStatus
    {
        NotStarted = 0,   // Chưa bắt đầu
        InProgress = 1,   // Đang học
        Completed = 2,    // Đã hoàn thành
        Locked = 3,       // Bị khóa (chưa unlock)
        Reviewed = 4      // Học lại / luyện tập thêm (sau khi đã Completed)
    }
}
