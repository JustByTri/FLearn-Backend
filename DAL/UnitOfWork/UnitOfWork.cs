using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using DAL.Repositories;

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
            AIFeedBacks = new AIFeedBackRepository(_context);
            ApplicationCertTypes = new ApplicationCertTypeRepository(_context);
            CertificateTypes = new CertificateTypeRepository(_context);
            ContentIssueReports = new ContentIssueReportRepository(_context);
            Conversations = new ConversationRepository(_context);
            Courses = new CourseRepository(_context);
            CourseGoals = new CourseGoalRepository(_context);
            CourseReviews = new CourseReviewRepository(_context);
            CourseSubmissions = new CourseSubmissionRepository(_context);
            CourseTemplates = new CourseTemplateRepository(_context);
            CourseTopics = new CourseTopicRepository(_context);
            CourseUnits = new CourseUnitRepository(_context);
            Enrollments = new EnrollmentRepository(_context);
            Exercises = new ExerciseRepository(_context);
            ExerciseEvaluationDetails = new ExerciseEvaluationDetailRepository(_context);
            ExerciseSubmissions = new ExerciseSubmissionRepository(_context);
            Goals = new GoalRepository(_context);
            Languages = new LanguageRepository(_context);
            LanguageLevels = new LanguageLevelRepository(_context);
            LearnerAchievements = new LearnerAchievementRepository(_context);
            LearnerLanguages = new LearnerLanguageRepository(_context);
            LearnerProgresses = new LearnerProgressRepository(_context);
            LearnerGoals = new LearnerGoalRepository(_context);
            Lessons = new LessonRepository(_context);
            LessonActivityLogs = new LessonActivityLogRepository(_context);
            LessonBookings = new LessonBookingRepository(_context);
            LessonDisputes = new LessonDisputeRepository(_context);
            LessonReviews = new LessonReviewRepository(_context);
            Messages = new MessageRepository(_context);
            PasswordResetOtps = new PasswordResetOtpRepository(_context);
            Purchases = new PurchaseRepository(_context);
            PurchaseDetails = new PurchaseDetailRepository(_context);
            RefreshTokens = new RefreshTokenRepository(_context);
            RegistrationOtps = new RegistrationOtpRepository(_context);
            Reviews = new ReviewRepository(_context);
            Roadmaps = new RoadmapRepository(_context);
            RoadmapDetails = new RoadmapDetailRepository(_context);
            Roles = new RoleRepository(_context);
            StaffLanguages = new StaffLanguageRepository(_context);
            TeacherApplications = new TeacherApplicationRepository(_context);
            TeacherPayouts = new TeacherPayoutRepository(_context);
            TeacherProfiles = new TeacherProfileRepository(_context);
            TeacherReviews = new TeacherReviewRepository(_context);
            TempRegistrations = new TempRegistrationRepository(_context);
            Topics = new TopicRepository(_context);
            UserTransactions = new TransactionRepository(_context);
            Users = new UserRepository(_context);
            UserRoles = new UserRoleRepository(_context);
            GlobalConversationPrompts = new GlobalConversationPromptRepository(_context);
            ConversationSessions = new ConversationSessionRepository(_context);
            ConversationMessages = new ConversationMessageRepository(_context);
            ConversationTasks  = new ConversationTaskRepository(_context);
            TeacherClasses = new TeacherClassRepository(_context);
            ClassDisputes = new ClassDisputeRepository(_context);
            ClassEnrollments = new ClassEnrollmentRepository(_context);
            RefundRequests = new RefundRequestsRepository(_context);


        }
        #region Repository Properties
        public IAchievementRepository Achievements { get; private set; }
        public IAIFeedBackRepository AIFeedBacks { get; private set; }
        public IApplicationCertTypeRepository ApplicationCertTypes { get; private set; }
        public ICertificateTypeRepository CertificateTypes { get; private set; }
        public IContentIssueReportRepository ContentIssueReports { get; private set; }
        public IConversationRepository Conversations { get; private set; }
        public ICourseRepository Courses { get; private set; }
        public ICourseGoalRepository CourseGoals { get; private set; }
        public ICourseReviewRepository CourseReviews { get; private set; }
        public ICourseSubmissionRepository CourseSubmissions { get; private set; }
        public ICourseTemplateRepository CourseTemplates { get; private set; }
        public ICourseTopicRepository CourseTopics { get; private set; }
        public ICourseUnitRepository CourseUnits { get; private set; }
        public IEnrollmentRepository Enrollments { get; private set; }
        public IExerciseRepository Exercises { get; private set; }
        public IExerciseEvaluationDetailRepository ExerciseEvaluationDetails { get; private set; }
        public IExerciseSubmissionRepository ExerciseSubmissions { get; private set; }
        public IGoalRepository Goals { get; private set; }
        public ILanguageRepository Languages { get; private set; }
        public ILanguageLevelRepository LanguageLevels { get; private set; }
        public ILearnerAchievementRepository LearnerAchievements { get; private set; }
        public ILearnerLanguageRepository LearnerLanguages { get; private set; }
        public ILearnerProgressRepository LearnerProgresses { get; private set; }
        public ILearnerGoalRepository LearnerGoals { get; private set; }
        public ILessonRepository Lessons { get; private set; }
        public ILessonActivityLogRepository LessonActivityLogs { get; private set; }
        public ILessonBookingRepository LessonBookings { get; private set; }
        public ILessonDisputeRepository LessonDisputes { get; private set; }
        public ILessonReviewRepository LessonReviews { get; private set; }
        public IMessageRepository Messages { get; private set; }
        public IPasswordResetOtpRepository PasswordResetOtps { get; private set; }
        public IPurchaseRepository Purchases { get; private set; }
        public IPurchaseDetailRepository PurchaseDetails { get; private set; }
        public IRefreshTokenRepository RefreshTokens { get; private set; }
        public IRegistrationOtpRepository RegistrationOtps { get; private set; }
        public IReviewRepository Reviews { get; private set; }
        public IRoadmapRepository Roadmaps { get; private set; }
        public IRoadmapDetailRepository RoadmapDetails { get; private set; }
        public IRoleRepository Roles { get; private set; }
        public IStaffLanguageRepository StaffLanguages { get; private set; }
        public ITeacherApplicationRepository TeacherApplications { get; private set; }
        public ITeacherPayoutRepository TeacherPayouts { get; private set; }
        public ITeacherProfileRepository TeacherProfiles { get; private set; }
        public ITeacherReviewRepository TeacherReviews { get; private set; }
        public ITempRegistrationRepository TempRegistrations { get; private set; }
        public ITopicRepository Topics { get; private set; }
        public ITransactionRepository UserTransactions { get; private set; }
        public IUserRepository Users { get; private set; }
        public IUserRoleRepository UserRoles { get; private set; }
        public IGlobalConversationPromptRepository GlobalConversationPrompts { get; private set; }
        public IConversationSessionRepository ConversationSessions { get; private set; }
        public IConversationMessageRepository ConversationMessages { get; private set; }
        public IConversationTaskRepository ConversationTasks { get; private set; }
        public ITeacherClassRepository TeacherClasses { get; private set; }
        public IClassDisputeRepository ClassDisputes { get; private set; }
        public IClassEnrollmentRepository ClassEnrollments { get; private set; }
        public IRefundRequestsRepository RefundRequests { get; private set; }
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
        #endregion
    }
}
