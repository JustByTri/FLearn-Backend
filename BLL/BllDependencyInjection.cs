
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


using DAL.DBContext;
using DAL;
using BLL.IServices.Auth;
using BLL.Services.Auth;
using BLL.IServices.Admin;
using BLL.Services.Admin;
namespace BLL
{
    public static class BllDependencyInjection
    {
        public static IServiceCollection AddBllServices(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IAdminService, AdminService>();

            services.AddDalServices(configuration);
            return services;
        }
    }
}
