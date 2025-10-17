using DAL.IRepositories;

namespace DAL.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IAchievementRepository Achievements { get; }
        IAIFeedBackRepository AIFeedBacks { get; }
        IApplicationCertTypeRepository ApplicationCertTypes { get; }
        ICertificateTypeRepository CertificateTypes { get; }
        IContentIssueReportRepository ContentIssueReports { get; }
        IConversationRepository Conversations { get; }
        ICourseRepository Courses { get; }
        ICourseGoalRepository CourseGoals { get; }
        ICourseReviewRepository CourseReviews { get; }
        ICourseSubmissionRepository CourseSubmissions { get; }
        ICourseTemplateRepository CourseTemplates { get; }
        ICourseTopicRepository CourseTopics { get; }
        ICourseUnitRepository CourseUnits { get; }
        IEnrollmentRepository Enrollments { get; }
        IExerciseRepository Exercises { get; }
        IExerciseEvaluationDetailRepository ExerciseEvaluationDetails { get; }
        IExerciseSubmissionRepository ExerciseSubmissions { get; }
        IGoalRepository Goals { get; }
        ILanguageRepository Languages { get; }
        ILanguageLevelRepository LanguageLevels { get; }
        ILearnerAchievementRepository LearnerAchievements { get; }
        ILearnerLanguageRepository LearnerLanguages { get; }
        ILearnerProgressRepository LearnerProgresses { get; }
        ILearnerGoalRepository LearnerGoals { get; }
        ILessonRepository Lessons { get; }
        ILessonActivityLogRepository LessonActivityLogs { get; }
        ILessonBookingRepository LessonBookings { get; }
        ILessonDisputeRepository LessonDisputes { get; }
        ILessonReviewRepository LessonReviews { get; }
        IMessageRepository Messages { get; }
        IPasswordResetOtpRepository PasswordResetOtps { get; }
        IPurchaseRepository Purchases { get; }
        IPurchaseDetailRepository PurchaseDetails { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        IRegistrationOtpRepository RegistrationOtps { get; }
        IReviewRepository Reviews { get; }
        IRoadmapRepository Roadmaps { get; }
        IRoadmapDetailRepository RoadmapDetails { get; }
        IRoleRepository Roles { get; }
        IStaffLanguageRepository StaffLanguages { get; }
        ITeacherApplicationRepository TeacherApplications { get; }
        ITeacherPayoutRepository TeacherPayouts { get; }
        ITeacherProfileRepository TeacherProfiles { get; }
        ITeacherReviewRepository TeacherReviews { get; }
        ITempRegistrationRepository TempRegistrations { get; }
        ITopicRepository Topics { get; }
        ITransactionRepository UserTransactions { get; }
        IUserRepository Users { get; }
        IUserRoleRepository UserRoles { get; }
        IGlobalConversationPromptRepository GlobalConversationPrompts { get; }
        IConversationSessionRepository ConversationSessions { get; }
        IConversationMessageRepository ConversationMessages { get; }
        IConversationTaskRepository ConversationTasks { get; }
        int SaveChanges();
        Task<int> SaveChangesAsync();
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
