using AutomationHub.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AutomationHub.Infrastructure.Data.Seed;

public class ApplicationContextSeed(ApplicationContext context)
{
    public async Task SeedAsync()
    {
        if (await context.Rules.AnyAsync()) return;

        var defaultRules = SeedRules.GetDefaultRules();

        context.Rules.AddRange(defaultRules);
        await context.SaveChangesAsync();
    }
}