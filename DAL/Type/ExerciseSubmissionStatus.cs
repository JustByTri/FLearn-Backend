namespace DAL.Type
{
    public enum ExerciseSubmissionStatus
    {
        PendingAiReview,     // Học viên vừa nộp, chờ AI chấm
        AIGraded,            // AI đã chấm xong, có điểm AI
        PendingTeacherReview,// AI chấm xong, chờ giáo viên chấm
        TeacherGraded,       // Giáo viên đã chấm
        NeedsRevision,       // Giáo viên yêu cầu học viên làm lại
        Passed,              // Giáo viên xác nhận đạt yêu cầu
        Failed               // Giáo viên xác nhận không đạt
    }
}
