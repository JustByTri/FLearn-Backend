
using BLL.IServices.Admin;
using BLL.IServices.AI;
using BLL.IServices.Auth;
using BLL.IServices.Course;
using BLL.IServices.CourseTemplate;
using BLL.IServices.CourseUnit;
using BLL.IServices.Exercise;
using BLL.IServices.Goal;
using BLL.IServices.Language;
using BLL.IServices.Lesson;
using BLL.IServices.Survey;
using BLL.IServices.Teacher;
using BLL.IServices.Topic;
using BLL.IServices.Upload;
using BLL.Services.Admin;
using BLL.Services.AI;
using BLL.Services.Auth;
using BLL.Services.Courses;
using BLL.Services.CourseTemplate;
using BLL.Services.CourseUnits;
using BLL.Services.Exercise;
using BLL.Services.Goal;
using BLL.Services.Languages;
using BLL.Services.Lessons;
using BLL.Services.Survey;
using BLL.Services.Teacher;
using BLL.Services.Topic;
using BLL.Services.Upload;
using BLL.Settings;
using DAL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddScoped<ITopicService, TopicService>();
            services.AddScoped<ICourseTemplateService, CourseTemplateService>();
            services.AddScoped<IGoalService, GoalService>();
            services.AddScoped<ILanguageService, LanguageService>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<ICourseUnitService, CourseUnitService>();
            services.AddScoped<ILessonService, LessonService>();
            services.AddScoped<IExerciseService, ExerciseService>();
            return services;
        }
    }
}
