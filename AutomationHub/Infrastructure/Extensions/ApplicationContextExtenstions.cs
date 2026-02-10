using AutomationHub.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AutomationHub.Infrastructure.Extensions;

public static class ApplicationContextExtensions
{
    public static IServiceCollection AddApplicationContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => 
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null
                    );
                    npgsqlOptions.CommandTimeout(30);
                }
            )
        );

        return services;
    }
}