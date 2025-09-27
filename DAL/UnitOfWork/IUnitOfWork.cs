using DAL.IRepositories;

namespace DAL.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IRoleRepository Roles { get; }
        IUserRoleRepository UserRoles { get; }
        ILanguageRepository Languages { get; }
        IUserLearningLanguageRepository UserLearningLanguages { get; }
        IAchievementRepository Achievements { get; }
        IUserAchievementRepository UserAchievements { get; }
        ICourseRepository Courses { get; }
        ICourseUnitRepository CourseUnits { get; }
        ILessonRepository Lessons { get; }
        IExerciseRepository Exercises { get; }
        IEnrollmentRepository Enrollments { get; }
        IPurchasesRepository Purchases { get; }
        IPurchasesDetailRepository PurchasesDetails { get; }
        ICourseTopicRepository CourseTopics { get; }
        ITopicRepository Topics { get; }
        ICourseSubmissionRepository CourseSubmissions { get; }
        ITeacherApplicationRepository TeacherApplications { get; }
        ITeacherCredentialRepository TeacherCredentials { get; }
        IRecordingRepository Recordings { get; }
        IReportRepository Reports { get; }
        IAIFeedBackRepository AIFeedBacks { get; }
        IConversationRepository Conversations { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        IRoadmapRepository Roadmaps { get; }
        IRoadmapDetailRepository RoadmapDetails { get; }
        IRegistrationOtpRepository RegistrationOtps { get; }
        ITempRegistrationRepository TempRegistrations { get; }
        IPasswordResetOtpRepository PasswordResetOtps { get; }
        IUserSurveyRepository UserSurveys { get; }
        IGoalRepository Goals { get; }
        ICourseTemplateRepository CourseTemplates { get; }
        IExerciseOptionRepository ExerciseOptions { get; }
        int SaveChanges();
        Task<int> SaveChangesAsync();
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
    }
}
