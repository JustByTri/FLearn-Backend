using DAL.IRepositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace DAL.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IAchievementRepository Achievements { get; }
        IApplicationCertTypeRepository ApplicationCertTypes { get; }
        ICertificateTypeRepository CertificateTypes { get; }
        IClassCancellationRequestRepository ClassCancellationRequests { get; }
        IClassDisputeRepository ClassDisputes { get; }
        IClassEnrollmentRepository ClassEnrollments { get; }
        IConversationMessageRepository ConversationMessages { get; }
        IConversationSessionRepository ConversationSessions { get; }
        IConversationTaskRepository ConversationTasks { get; }
        ICourseRepository Courses { get; }
        ICourseReviewRepository CourseReviews { get; }
        ICourseSubmissionRepository CourseSubmissions { get; }
        ICourseTemplateRepository CourseTemplates { get; }
        ICourseTopicRepository CourseTopics { get; }
        ICourseUnitRepository CourseUnits { get; }
        IEnrollmentRepository Enrollments { get; }
        IExerciseRepository Exercises { get; }
        IExerciseGradingAssignmentRepository ExerciseGradingAssignments { get; }
        IExerciseSubmissionRepository ExerciseSubmissions { get; }
        IGlobalConversationPromptRepository GlobalConversationPrompts { get; }
        ILanguageRepository Languages { get; }
        ILanguageLevelRepository LanguageLevels { get; }
        ILearnerAchievementRepository LearnerAchievements { get; }
        ILearnerLanguageRepository LearnerLanguages { get; }
        ILearnerXpEventRepository LearnerXpEvents { get; }
        ILessonRepository Lessons { get; }
        ILessonActivityLogRepository LessonActivityLogs { get; }
        ILessonProgressRepository LessonProgresses { get; }
        ILevelRepository Levels { get; }
        IManagerLanguageRepository ManagerLanguages { get; }
        IPasswordResetOtpRepository PasswordResetOtps { get; }
        IPaymentTransactionRepository PaymentTransactions { get; }
        IPayoutRequestRepository PayoutRequests { get; }
        IPurchaseRepository Purchases { get; }
        IProgramRepository Programs { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        IRefundRequestsRepository RefundRequests { get; }
        IRegistrationOtpRepository RegistrationOtps { get; }
        IReviewRepository Reviews { get; }
        IRoleRepository Roles { get; }
        ITeacherApplicationRepository TeacherApplications { get; }
        ITeacherBankAccountRepository TeacherBankAccounts { get; }
        ITeacherClassRepository TeacherClasses { get; }
        ITeacherEarningAllocationRepository TeacherEarningAllocations { get; }
        ITeacherPayoutRepository TeacherPayouts { get; }
        ITeacherProfileRepository TeacherProfiles { get; }
        ITeacherProgramAssignmentRepository TeacherProgramAssignments { get; }
        ITeacherReviewRepository TeacherReviews { get; }
        ITempRegistrationRepository TempRegistrations { get; }
        ITopicRepository Topics { get; }
        IUnitProgressRepository UnitProgresses { get; }
        IUserRepository Users { get; }
        IUserRoleRepository UserRoles { get; }
        IUserSubscriptionRepository UserSubscriptions { get; }
        IWalletRepository Wallets { get; }
        IWalletTransactionRepository WalletTransactions { get; }
        int SaveChanges();
        Task<int> SaveChangesAsync();
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task ExecuteInTransactionAsync(Func<Task> operation);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
        IExecutionStrategy CreateExecutionStrategy();
    }
}
