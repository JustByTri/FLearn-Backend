namespace DAL.Type
{
    public enum ExerciseSubmissionStatus
    {
        PendingAIReview = 1,
        AIGraded = 2,
        PendingTeacherReview = 3,
        TeacherGraded = 4,
        Passed = 5,
        Failed = 6
    }
}
