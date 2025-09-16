
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

        
            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

            
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<ILanguageRepository, LanguageRepository>();
            services.AddScoped<IUserLearningLanguageRepository, UserLearningLanguageRepository>();
            services.AddScoped<IAchievementRepository, AchievementRepository>();
            services.AddScoped<IUserAchievementRepository, UserAchievementRepository>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<ICourseUnitRepository, CourseUnitRepository>();
            services.AddScoped<ILessonRepository, LessonRepository>();
            services.AddScoped<IExerciseRepository, ExerciseRepository>();
            services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
            services.AddScoped<IPurchasesRepository, PurchasesRepository>();
            services.AddScoped<IPurchasesDetailRepository, PurchasesDetailRepository>();
            services.AddScoped<ICourseTopicRepository, CourseTopicRepository>();
            services.AddScoped<ITopicRepository, TopicRepository>();
            services.AddScoped<ICourseSubmissionRepository, CourseSubmissionRepository>();
            services.AddScoped<ITeacherApplicationRepository, TeacherApplicationRepository>();
            services.AddScoped<ITeacherCredentialRepository, TeacherCredentialRepository>();
            services.AddScoped<IRecordingRepository, RecordingRepository>();
            services.AddScoped<IReportRepository, ReportRepository>();
            services.AddScoped<IAIFeedBackRepository, AIFeedBackRepository>();
            services.AddScoped<IConversationRepository, ConversationRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IRoadmapRepository, RoadmapRepository>();
            services.AddScoped<IRoadmapDetailRepository, RoadmapDetailRepository>();

            return services;
        }
    }
}
