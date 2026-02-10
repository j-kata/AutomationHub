using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using AutomationHub.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AutomationHub.Infrastructure.Data.Repositories;

public class RuleDbRepository(ApplicationContext context) : IRuleRepository
{

    public async Task<IEnumerable<Rule>> GetRulesForEvent(EventType eventType, string? source) =>
        await context.Rules
            .Include(r => r.Actions)
            .Where(r => r.EventType == eventType)
            .Where(r => r.Source == null ||
                        r.Source == "*" ||
                        r.Source == source
            )
            .OrderByDescending(r => r.Priority)
            .ToListAsync();
}