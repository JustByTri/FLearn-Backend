using BLL.Background;
using BLL.HostedServices; // added
using BLL.IServices.Admin;
using BLL.IServices.AI;
using BLL.IServices.Application;
using BLL.IServices.AppReview;
using BLL.IServices.Assessment;
using BLL.IServices.Auth;
using BLL.IServices.Certificate;
using BLL.IServices.Course;
using BLL.IServices.CourseTemplate;
using BLL.IServices.CourseUnit;
using BLL.IServices.Coversation;
using BLL.IServices.Dashboard;
using BLL.IServices.Dispute;
using BLL.IServices.Enrollment;
using BLL.IServices.Exercise;
using BLL.IServices.FirebaseService;
using BLL.IServices.Gamification;
using BLL.IServices.Language;
using BLL.IServices.Lesson;
using BLL.IServices.Payment;
using BLL.IServices.ProgressTracking;
using BLL.IServices.Purchases;
using BLL.IServices.Redis;
using BLL.IServices.Refund;
using BLL.IServices.Subscription;
using BLL.IServices.Teacher;
using BLL.IServices.TeacherReview;
using BLL.IServices.Topic;
using BLL.IServices.Upload;
using BLL.IServices.Wallets;
using BLL.IServices.WalletTransactions;
using BLL.Services;
using BLL.Services.Admin;
using BLL.Services.AI;
using BLL.Services.Application;
using BLL.Services.AppReview;
using BLL.Services.Assessment;
using BLL.Services.Auth;
using BLL.Services.Certificate;
using BLL.Services.Course;
using BLL.Services.CourseReview;
using BLL.Services.CourseTemplate;
using BLL.Services.CourseUnits;
using BLL.Services.Dashboard;
using BLL.Services.Dispute;
using BLL.Services.Enrollment;
using BLL.Services.Exercise;
using BLL.Services.FirebaseService;
using BLL.Services.Gamification;
using BLL.Services.Languages;
using BLL.Services.Lesson;
using BLL.Services.Payment;
using BLL.Services.ProgressTracking;
using BLL.Services.Purchases;
using BLL.Services.Redis;
using BLL.Services.Refund;
using BLL.Services.Subscription;
using BLL.Services.Teacher;
using BLL.Services.TeacherReview;
using BLL.Services.Topic;
using BLL.Services.Upload;
using BLL.Services.Wallets;
using BLL.Services.WalletTransactions;
using BLL.Settings;
using Common.Authorization;
using DAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // added
using StackExchange.Redis;

namespace BLL
{
    public static class BllDependencyInjection
    {
        public static IServiceCollection AddBllServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IClassAdminService, ClassAdminService>();
            services.AddScoped<IPayoutAdminService, PayoutAdminService>();
            services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));
            services.Configure<RedisSettings>(configuration.GetSection("RedisSettings"));
            services.Configure<AzureOpenAISettings>(configuration.GetSection("AzureOpenAISettings"));
            services.Configure<SpeechSettings>(configuration.GetSection("SpeechSettings"));

            services.AddDalServices(configuration);

            //REDIS SETUP
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var redisSettings = configuration.GetSection("RedisSettings");
                var connectionString = redisSettings.GetValue<string>("ConnectionString");
                var password = redisSettings.GetValue<string>("Password");

                var configOptions = new ConfigurationOptions
                {
                    EndPoints = { connectionString! },
                    AbortOnConnectFail = false,
                    ConnectRetry = 3,
                    ConnectTimeout = 5000
                };

                if (!string.IsNullOrEmpty(password))
                {
                    configOptions.Password = password;
                }

                return ConnectionMultiplexer.Connect(configOptions);
            });

            services.AddStackExchangeRedisCache(options =>
            {
                var redisSettings = configuration.GetSection("RedisSettings");
                var connectionString = redisSettings.GetValue<string>("ConnectionString");
                var password = redisSettings.GetValue<string>("Password");

                options.Configuration = connectionString;
                options.InstanceName = redisSettings.GetValue<string>("InstanceName") ?? "FLearnApp_";

                if (!string.IsNullOrEmpty(password))
                {
                    options.ConfigurationOptions = new ConfigurationOptions
                    {
                        EndPoints = { connectionString! },
                        Password = password,
                        AbortOnConnectFail = false,
                        ConnectRetry = 3
                    };
                }
            });
            services.AddSignalR();
            services.AddScoped<IRedisService, RedisService>();
            services.AddScoped<ICloudinaryService, CloudinaryService>();

            services.AddHttpClient<IGeminiService, AzureOpenAIService>();
            services.AddHttpClient<AzureOpenAITranscriptionService>();
            services.AddScoped<AzureSpeechTranscriptionService>();

            // Composite STT: try Azure Speech for WAV (no GStreamer needed), fallback to Azure OpenAI Whisper
            services.AddScoped<ITranscriptionService>(sp =>
            {
                var speech = sp.GetRequiredService<AzureSpeechTranscriptionService>();
                var openai = sp.GetRequiredService<AzureOpenAITranscriptionService>();
                var logger = sp.GetRequiredService<ILogger<CompositeTranscriptionService>>();
                return new CompositeTranscriptionService(speech, openai, logger);
            });

            services.AddScoped<IPronunciationAssessmentService, AzureSpeechPronunciationAssessmentService>();

            // ensure VoiceAssessmentService gets STT
            services.AddScoped<IVoiceAssessmentService, VoiceAssessmentService>();

            services.AddScoped<ITopicService, TopicService>();
            services.AddScoped<ICourseTemplateService, CourseTemplateService>();
            services.AddScoped<ILanguageService, LanguageService>();
            services.AddScoped<ICertificateService, CertificateService>();
            services.AddSingleton<IAuthorizationHandler, ExclusiveRoleHandler>();
            services.AddScoped<ITeacherApplicationService, TeacherApplicationService>();

            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<ICourseUnitService, CourseUnitService>();
            services.AddScoped<ILessonService, LessonService>();
            services.AddScoped<IExerciseService, ExerciseService>();

            services.AddScoped<IConversationPartnerService, ConversationPartnerService>();
            services.AddScoped<IClassEnrollmentService, ClassEnrollmentService>();
            services.AddScoped<IPayOSService, PayOSService>();
            services.AddHttpClient<PayOSService>();
            services.AddScoped<ITeacherClassService, TeacherClassService>();
            services.AddHostedService<ClassEnrollmentCheckService>();
            services.AddScoped<IRefundRequestService, RefundRequestService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<ITeacherService, TeacherService>();
            services.AddScoped<IPurchaseService, PurchaseService>();
            services.AddScoped<IPaymentService, PayOSPaymentService>();
            services.AddScoped<IWalletService, WalletService>();
            services.AddScoped<WalletService>();
            services.AddScoped<IEnrollmentService, EnrollmentService>();
            services.AddScoped<IAssessmentService, AssessmentService>();
            services.AddScoped<IProgressTrackingService, ProgressTrackingService>();
            services.AddScoped<IExerciseGradingService, ExerciseGradingService>();
            services.AddScoped<ILessonProgressService, LessonProgressService>();
            services.AddHostedService<DailyConversationResetService>();
            services.AddHostedService<SubscriptionExpiryService>();
            services.AddScoped<ClassLifecycleHangfireJob>();
            services.AddScoped<IPronunciationService, PronunciationService>();
            services.AddScoped<IGamificationService, GamificationService>();
            services.AddScoped<IManagerDashboardService, ManagerDashboardService>();
            services.AddScoped<ICourseReviewService, CourseReviewService>();
            services.AddScoped<ITeacherReviewService, TeacherReviewService>();
            services.AddScoped<IAIContentModerationService, AIContentModerationService>();
            services.AddScoped<IAppReviewService, AppReviewService>();
            services.AddScoped<IClassDisputeService, ClassDisputeService>(); 
            
            services.AddScoped<IFirebaseNotificationService, FirebaseNotificationService>();
            services.AddHostedService<ClassNotificationBackgroundService>();
            
            services.AddScoped<IWalletTransactionService, WalletTransactionService>();
            return services;
        }
    }
}

