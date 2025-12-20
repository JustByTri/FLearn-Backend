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

            // Register all repositories
            services.AddScoped<IAchievementRepository, AchievementRepository>();
            services.AddScoped<IApplicationCertTypeRepository, ApplicationCertTypeRepository>();
            services.AddScoped<ICertificateTypeRepository, CertificateTypeRepository>();
            services.AddScoped<IClassDisputeRepository, ClassDisputeRepository>();
            services.AddScoped<IClassEnrollmentRepository, ClassEnrollmentRepository>();
            services.AddScoped<IConversationMessageRepository, ConversationMessageRepository>();
            services.AddScoped<IConversationSessionRepository, ConversationSessionRepository>();
            services.AddScoped<IConversationTaskRepository, ConversationTaskRepository>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<ICourseReviewRepository, CourseReviewRepository>();
            services.AddScoped<ICourseSubmissionRepository, CourseSubmissionRepository>();
            services.AddScoped<ICourseTemplateRepository, CourseTemplateRepository>();
            services.AddScoped<ICourseTopicRepository, CourseTopicRepository>();
            services.AddScoped<ICourseUnitRepository, CourseUnitRepository>();
            services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
            services.AddScoped<IExerciseRepository, ExerciseRepository>();
            services.AddScoped<IExerciseGradingAssignmentRepository, ExerciseGradingAssignmentRepository>();
            services.AddScoped<IExerciseSubmissionRepository, ExerciseSubmissionRepository>();
            services.AddScoped<IGlobalConversationPromptRepository, GlobalConversationPromptRepository>();
            services.AddScoped<ILanguageRepository, LanguageRepository>();
            services.AddScoped<ILanguageLevelRepository, LanguageLevelRepository>();
            services.AddScoped<ILearnerAchievementRepository, LearnerAchievementRepository>();
            services.AddScoped<ILearnerLanguageRepository, LearnerLanguageRepository>();
            services.AddScoped<ILessonRepository, LessonRepository>();
            services.AddScoped<ILessonActivityLogRepository, LessonActivityLogRepository>();
            services.AddScoped<ILessonProgressRepository, LessonProgressRepository>();
            services.AddScoped<ILevelRepository, LevelRepository>();
            services.AddScoped<IManagerLanguageRepository, ManagerLanguageRepository>();
            services.AddScoped<IPasswordResetOtpRepository, PasswordResetOtpRepository>();
            services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
            services.AddScoped<IPayoutRequestRepository, PayoutRequestRepository>();
            services.AddScoped<IPurchaseRepository, PurchaseRepository>();
            services.AddScoped<IProgramRepository, ProgramRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IRefundRequestsRepository, RefundRequestsRepository>();
            services.AddScoped<IRegistrationOtpRepository, RegistrationOtpRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<ITeacherApplicationRepository, TeacherApplicationRepository>();
            services.AddScoped<ITeacherBankAccountRepository, TeacherBankAccountRepository>();
            services.AddScoped<ITeacherClassRepository, TeacherClassRepository>();
            services.AddScoped<ITeacherEarningAllocationRepository, TeacherEarningAllocationRepository>();
            services.AddScoped<ITeacherPayoutRepository, TeacherPayoutRepository>();
            services.AddScoped<ITeacherProfileRepository, TeacherProfileRepository>();
            services.AddScoped<ITeacherProgramAssignmentRepository, TeacherProgramAssignmentRepository>();
            services.AddScoped<ITeacherReviewRepository, TeacherReviewRepository>();
            services.AddScoped<ITempRegistrationRepository, TempRegistrationRepository>();
            services.AddScoped<ITopicRepository, TopicRepository>();
            services.AddScoped<IUnitProgressRepository, UnitProgressRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            // Register UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

            return services;
        }
    }
}
