using DAL.DBContext;
using DAL.IRepositories;
using DAL.Repositories;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DAL
{
    public static class DalDependencyInjection
    {
        public static IServiceCollection AddDalServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register DbContext
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseMySql(
                    configuration.GetConnectionString("DefaultConnection"),
                    ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection")),
                    mysqlOptions => mysqlOptions.EnableRetryOnFailure());
            });

            // UnitOfWork registration
            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
            // Repository DI registration
            services.AddScoped<IAchievementRepository, AchievementRepository>();
            services.AddScoped<IAIFeedBackRepository, AIFeedBackRepository>();
            services.AddScoped<IApplicationCertTypeRepository, ApplicationCertTypeRepository>();
            services.AddScoped<ICertificateTypeRepository, CertificateTypeRepository>();
            services.AddScoped<IContentIssueReportRepository, ContentIssueReportRepository>();
            services.AddScoped<IConversationRepository, ConversationRepository>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<ICourseGoalRepository, CourseGoalRepository>();
            services.AddScoped<ICourseReviewRepository, CourseReviewRepository>();
            services.AddScoped<ICourseSubmissionRepository, CourseSubmissionRepository>();
            services.AddScoped<ICourseTemplateRepository, CourseTemplateRepository>();
            services.AddScoped<ICourseTopicRepository, CourseTopicRepository>();
            services.AddScoped<ICourseUnitRepository, CourseUnitRepository>();
            services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
            services.AddScoped<IExerciseRepository, ExerciseRepository>();
            services.AddScoped<IExerciseEvaluationDetailRepository, ExerciseEvaluationDetailRepository>();
            services.AddScoped<IExerciseSubmissionRepository, ExerciseSubmissionRepository>();
            services.AddScoped<IGoalRepository, GoalRepository>();
            services.AddScoped<ILanguageRepository, LanguageRepository>();
            services.AddScoped<ILanguageLevelRepository, LanguageLevelRepository>();
            services.AddScoped<ILearnerAchievementRepository, LearnerAchievementRepository>();
            services.AddScoped<ILearnerLanguageRepository, LearnerLanguageRepository>();
            services.AddScoped<ILearnerProgressRepository, LearnerProgressRepository>();
            services.AddScoped<ILearnerGoalRepository, LearnerGoalRepository>();
            services.AddScoped<ILessonRepository, LessonRepository>();
            services.AddScoped<ILessonActivityLogRepository, LessonActivityLogRepository>();
            services.AddScoped<ILessonBookingRepository, LessonBookingRepository>();
            services.AddScoped<ILessonDisputeRepository, LessonDisputeRepository>();
            services.AddScoped<ILessonReviewRepository, LessonReviewRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IPasswordResetOtpRepository, PasswordResetOtpRepository>();
            services.AddScoped<IPurchaseRepository, PurchaseRepository>();
            services.AddScoped<IPurchaseDetailRepository, PurchaseDetailRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IRegistrationOtpRepository, RegistrationOtpRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IRoadmapRepository, RoadmapRepository>();
            services.AddScoped<IRoadmapDetailRepository, RoadmapDetailRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IStaffLanguageRepository, StaffLanguageRepository>();
            services.AddScoped<ITeacherApplicationRepository, TeacherApplicationRepository>();
            services.AddScoped<ITeacherPayoutRepository, TeacherPayoutRepository>();
            services.AddScoped<ITeacherProfileRepository, TeacherProfileRepository>();
            services.AddScoped<ITeacherReviewRepository, TeacherReviewRepository>();
            services.AddScoped<ITempRegistrationRepository, TempRegistrationRepository>();
            services.AddScoped<ITopicRepository, TopicRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            return services;
        }
    }
}
