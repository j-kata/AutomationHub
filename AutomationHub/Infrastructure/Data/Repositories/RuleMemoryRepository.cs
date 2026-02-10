using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using AutomationHub.Infrastructure.Data.Seed;

namespace AutomationHub.Infrastructure.Data.Repositories;

public class RuleMemoryRepository : IRuleRepository
{
    private readonly List<Rule> _rules = [];

    public RuleMemoryRepository()
    {
        _rules = SeedRules.GetDefaultRules();
    }

    public Task<IEnumerable<Rule>> GetRulesForEvent(EventType eventType, string? source)
    {
        var rules = _rules
            .Where(r => r.EventType == eventType)
            .Where(r => r.Source == null ||
                        r.Source == "*" ||
                        r.Source == source
            )
            .OrderByDescending(r => r.Priority);

        return Task.FromResult<IEnumerable<Rule>>(rules);
    }
}