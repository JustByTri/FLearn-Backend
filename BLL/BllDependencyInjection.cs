
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


using DAL.DBContext;
using DAL;
namespace BLL
{
    public static class BllDependencyInjection
    {
        public static IServiceCollection AddBllServices(this IServiceCollection services, IConfiguration configuration)
        {


            services.AddDalServices(configuration);
            return services;
        }
    }
}
