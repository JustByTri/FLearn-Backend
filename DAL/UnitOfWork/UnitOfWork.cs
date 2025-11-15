using DAL.DBContext;
using DAL.IRepositories;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DAL.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction _transaction;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Achievements = new AchievementRepository(_context);
            ApplicationCertTypes = new ApplicationCertTypeRepository(_context);
            CertificateTypes = new CertificateTypeRepository(_context);
            ClassDisputes = new ClassDisputeRepository(_context);
            ClassEnrollments = new ClassEnrollmentRepository(_context);
            ConversationMessages = new ConversationMessageRepository(_context);
            ConversationSessions = new ConversationSessionRepository(_context);
            ConversationTasks = new ConversationTaskRepository(_context);
            Courses = new CourseRepository(_context);
            CourseReviews = new CourseReviewRepository(_context);
            CourseSubmissions = new CourseSubmissionRepository(_context);
            CourseTemplates = new CourseTemplateRepository(_context);
            CourseTopics = new CourseTopicRepository(_context);
            CourseUnits = new CourseUnitRepository(_context);
            Enrollments = new EnrollmentRepository(_context);
            Exercises = new ExerciseRepository(_context);
            ExerciseGradingAssignments = new ExerciseGradingAssignmentRepository(_context);
            ExerciseSubmissions = new ExerciseSubmissionRepository(_context);
            GlobalConversationPrompts = new GlobalConversationPromptRepository(_context);
            Languages = new LanguageRepository(_context);
            LanguageLevels = new LanguageLevelRepository(_context);
            LearnerAchievements = new LearnerAchievementRepository(_context);
            LearnerLanguages = new LearnerLanguageRepository(_context);
            LearnerXpEvents = new LearnerXpEventRepository(_context);
            Lessons = new LessonRepository(_context);
            LessonActivityLogs = new LessonActivityLogRepository(_context);
            LessonProgresses = new LessonProgressRepository(_context);
            Levels = new LevelRepository(_context);
            ManagerLanguages = new ManagerLanguageRepository(_context);
            PasswordResetOtps = new PasswordResetOtpRepository(_context);
            PaymentTransactions = new PaymentTransactionRepository(_context);
            PayoutRequests = new PayoutRequestRepository(_context);
            Purchases = new PurchaseRepository(_context);
            Programs = new ProgramRepository(_context);
            RefreshTokens = new RefreshTokenRepository(_context);
            RefundRequests = new RefundRequestsRepository(_context);
            RegistrationOtps = new RegistrationOtpRepository(_context);
            Reviews = new ReviewRepository(_context);
            Roles = new RoleRepository(_context);
            TeacherApplications = new TeacherApplicationRepository(_context);
            TeacherBankAccounts = new TeacherBankAccountRepository(_context);
            TeacherClasses = new TeacherClassRepository(_context);
            TeacherEarningAllocations = new TeacherEarningAllocationRepository(_context);
            TeacherPayouts = new TeacherPayoutRepository(_context);
            TeacherProfiles = new TeacherProfileRepository(_context);
            TeacherProgramAssignments = new TeacherProgramAssignmentRepository(_context);
            TeacherReviews = new TeacherReviewRepository(_context);
            TempRegistrations = new TempRegistrationRepository(_context);
            Topics = new TopicRepository(_context);
            UnitProgresses = new UnitProgressRepository(_context);
            Users = new UserRepository(_context);
            UserRoles = new UserRoleRepository(_context);
            UserSubscriptions = new UserSubscriptionRepository(_context);
            Wallets = new WalletRepository(_context);
            WalletTransactions = new WalletTransactionRepository(_context);
        }
        #region Repository Properties
        public IAchievementRepository Achievements { get; private set; }
        public IApplicationCertTypeRepository ApplicationCertTypes { get; private set; }
        public ICertificateTypeRepository CertificateTypes { get; private set; }
        public IClassDisputeRepository ClassDisputes { get; private set; }
        public IClassEnrollmentRepository ClassEnrollments { get; private set; }
        public IConversationMessageRepository ConversationMessages { get; private set; }
        public IConversationSessionRepository ConversationSessions { get; private set; }
        public IConversationTaskRepository ConversationTasks { get; private set; }
        public ICourseRepository Courses { get; private set; }
        public ICourseReviewRepository CourseReviews { get; private set; }
        public ICourseSubmissionRepository CourseSubmissions { get; private set; }
        public ICourseTemplateRepository CourseTemplates { get; private set; }
        public ICourseTopicRepository CourseTopics { get; private set; }
        public ICourseUnitRepository CourseUnits { get; private set; }
        public IEnrollmentRepository Enrollments { get; private set; }
        public IExerciseRepository Exercises { get; private set; }
        public IExerciseGradingAssignmentRepository ExerciseGradingAssignments { get; private set; }
        public IExerciseSubmissionRepository ExerciseSubmissions { get; private set; }
        public IGlobalConversationPromptRepository GlobalConversationPrompts { get; private set; }
        public ILanguageRepository Languages { get; private set; }
        public ILanguageLevelRepository LanguageLevels { get; private set; }
        public ILearnerAchievementRepository LearnerAchievements { get; private set; }
        public ILearnerLanguageRepository LearnerLanguages { get; private set; }
        public ILearnerXpEventRepository LearnerXpEvents { get; private set; }
        public ILessonRepository Lessons { get; private set; }
        public ILessonActivityLogRepository LessonActivityLogs { get; private set; }
        public ILessonProgressRepository LessonProgresses { get; private set; }
        public ILevelRepository Levels { get; private set; }
        public IManagerLanguageRepository ManagerLanguages { get; private set; }
        public IPasswordResetOtpRepository PasswordResetOtps { get; private set; }
        public IPaymentTransactionRepository PaymentTransactions { get; private set; }
        public IPayoutRequestRepository PayoutRequests { get; private set; }
        public IPurchaseRepository Purchases { get; private set; }
        public IProgramRepository Programs { get; private set; }
        public IRefreshTokenRepository RefreshTokens { get; private set; }
        public IRefundRequestsRepository RefundRequests { get; private set; }
        public IRegistrationOtpRepository RegistrationOtps { get; private set; }
        public IReviewRepository Reviews { get; private set; }
        public IRoleRepository Roles { get; private set; }
        public ITeacherApplicationRepository TeacherApplications { get; private set; }
        public ITeacherBankAccountRepository TeacherBankAccounts { get; private set; }
        public ITeacherClassRepository TeacherClasses { get; private set; }
        public ITeacherEarningAllocationRepository TeacherEarningAllocations { get; private set; }
        public ITeacherPayoutRepository TeacherPayouts { get; private set; }
        public ITeacherProfileRepository TeacherProfiles { get; private set; }
        public ITeacherProgramAssignmentRepository TeacherProgramAssignments { get; private set; }
        public ITeacherReviewRepository TeacherReviews { get; private set; }
        public ITempRegistrationRepository TempRegistrations { get; private set; }
        public ITopicRepository Topics { get; private set; }
        public IUnitProgressRepository UnitProgresses { get; private set; }
        public IUserRepository Users { get; private set; }
        public IUserRoleRepository UserRoles { get; private set; }
        public IUserSubscriptionRepository UserSubscriptions { get; private set; }
        public IWalletRepository Wallets { get; private set; }
        public IWalletTransactionRepository WalletTransactions { get; private set; }
        #endregion
        #region Transaction Methods
        public void BeginTransaction()
        {
            _transaction = _context.Database.BeginTransaction();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public void CommitTransaction()
        {
            _transaction?.Commit();
            _transaction?.Dispose();
            _transaction = null;
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void RollbackTransaction()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _transaction = null;
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
        #endregion

        #region SaveChanges
        public int SaveChanges() => _context.SaveChanges();
        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
        #endregion

        #region Dispose
        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            var strategy = CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await operation();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            var strategy = CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var result = await operation();
                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public IExecutionStrategy CreateExecutionStrategy()
        {
            return _context.Database.CreateExecutionStrategy();
        }
        #endregion
    }
}
