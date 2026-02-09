using AutomationHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutomationHub.Infrastructure.Extensions;

public static class ApplicationContextExtensions
{
    public static IServiceCollection AddApplicationContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}