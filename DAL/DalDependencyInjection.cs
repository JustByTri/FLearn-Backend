
using DAL.DBContext;
using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

namespace DAL
{
    public static class DalDependencyInjection
    {
        public static IServiceCollection AddDalServices(this IServiceCollection services, IConfiguration configuration)
        {
          
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseMySql(
                    configuration.GetConnectionString("DefaultConnection"),
                    ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection")),
                    mysqlOptions => mysqlOptions.EnableRetryOnFailure());

            });
            return services;
        }
    }
}
