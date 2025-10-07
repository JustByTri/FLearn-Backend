using BLL.IServices.Admin;
using BLL.IServices.AI;
using BLL.IServices.Assessment;
using BLL.IServices.Auth;
using BLL.IServices.CourseTemplate;
using BLL.IServices.Goal;
using BLL.IServices.Language;
using BLL.IServices.Redis;
using BLL.IServices.Topic;
using BLL.IServices.Upload;
using BLL.Services.Admin;
using BLL.Services.AI;
using BLL.Services.Assessment;
using BLL.Services.Auth;
using BLL.Services.CourseTemplate;
using BLL.Services.Goal;
using BLL.Services.Languages;
using BLL.Services.Redis;
using BLL.Services.Topic;
using BLL.Services.Upload;
using BLL.Settings;
using DAL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));
            services.Configure<RedisSettings>(configuration.GetSection("RedisSettings"));

            services.AddDalServices(configuration);

            // 🚀 REDIS SETUP
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

            services.AddScoped<IRedisService, RedisService>();

            services.AddScoped<ICloudinaryService, CloudinaryService>();
            services.AddScoped<IGeminiService, GeminiService>();
            services.AddHttpClient<IGeminiService, GeminiService>();
            services.Configure<GeminiSettings>(configuration.GetSection("GeminiSettings"));
            services.AddScoped<ITopicService, TopicService>();
            services.AddScoped<ICourseTemplateService, CourseTemplateService>();
            services.AddScoped<IGoalService, GoalService>();
            services.AddScoped<ILanguageService, LanguageService>();
            services.AddScoped<IVoiceAssessmentService, VoiceAssessmentService>();
            return services;
        }
    }
}

