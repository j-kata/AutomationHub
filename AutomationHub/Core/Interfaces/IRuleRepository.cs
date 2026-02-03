using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Core.Interfaces;

public interface IRuleRepository
{
    Task<IEnumerable<Rule>> GetRulesForEvent(EventType eventType, string source);
}