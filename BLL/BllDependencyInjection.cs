
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


using DAL.DBContext;
using DAL;
using BLL.IServices.Auth;
using BLL.Services.Auth;
using BLL.IServices.Admin;
using BLL.Services.Admin;
using BLL.IServices.Teacher;
using BLL.IServices.Upload;
using BLL.Services.Teacher;
using BLL.Services.Upload;
using BLL.Settings;
using BLL.IServices.AI;
using BLL.IServices.Survey;
using BLL.Services.AI;
using BLL.Services.Survey;
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
            services.AddDalServices(configuration);
            services.AddScoped<ICloudinaryService, CloudinaryService>();
            services.AddScoped<IGeminiService, GeminiService>();
            services.AddScoped<IUserSurveyService, UserSurveyService>();
            services.AddHttpClient<IGeminiService, GeminiService>();
            services.Configure<GeminiSettings>(configuration.GetSection("GeminiSettings"));
            services.AddScoped<ITeacherApplicationService, TeacherApplicationService>();
            return services;
        }
    }
}
