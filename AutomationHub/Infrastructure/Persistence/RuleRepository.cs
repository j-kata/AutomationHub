using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Infrastructure.Persistence;

public class RuleRepository : IRuleRepository
{
    private readonly List<Rule> _rules = [];

    public RuleRepository()
    {
        _rules = GenerateRules();
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

    private static List<Rule> GenerateRules()
    {
        var globalRuleId = Guid.NewGuid();

        return [
            GenerateRule(
                ruleId: globalRuleId,
                eventType: EventType.TemperatureReading,
                source: null,
                condition: "temperature > 50",
                priority: Priority.High,
                actionType: ActionType.LogEvent,
                message: "Extreme temperature detected!"
            ),

            GenerateRule(
                ruleId: Guid.NewGuid(),
                eventType: EventType.TemperatureReading,
                source: "kitchen-sensor",
                condition: "temperature > 30",
                priority: Priority.Medium,
                actionType: ActionType.LogEvent,
                message: "Kitchen temperature high"
            ),

            GenerateRule(
                ruleId: Guid.NewGuid(),
                eventType: EventType.TemperatureReading,
                source: "bedroom-sensor",
                condition: "temperature > 22",
                priority: Priority.Medium,
                actionType: ActionType.LogEvent,
                message: "Bedroom too warm for sleeping"
            ),

            GenerateRule(
                ruleId: globalRuleId,
                eventType: EventType.MotionDetected,
                source: null,
                condition: null,
                priority: Priority.Low,
                actionType: ActionType.LogEvent,
                message: "Motion detected"
            ),
        ];
    }

    private static Rule GenerateRule(
        Guid ruleId, EventType eventType, string? source, string? condition, Priority priority, ActionType actionType, string message)
    {
        return new()
        {
            Id = ruleId,
            EventType = eventType,
            Source = source,
            Condition = condition,
            Priority = priority,
            Actions =
            [
                new() {
                    Id = Guid.NewGuid(),
                    RuleId = ruleId,
                    ActionType = actionType,
                    Parameters = new() { ["message"] = message }
                }
            ]
        };
    }
}