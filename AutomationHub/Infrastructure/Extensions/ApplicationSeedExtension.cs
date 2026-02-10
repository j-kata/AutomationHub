using AutomationHub.Infrastructure.Data.Contexts;
using AutomationHub.Infrastructure.Data.Seed;

namespace AutomationHub.Infrastructure.Extensions;

public static class ApplicationSeedExtensions
{
    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        await context.Database.EnsureCreatedAsync();

        var seeder = new ApplicationContextSeed(context);
        await seeder.SeedAsync();
    }
}