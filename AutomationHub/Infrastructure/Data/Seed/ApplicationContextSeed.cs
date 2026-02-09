using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using AutomationHub.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AutomationHub.Infrastructure.Data.Seed;

public class ApplicationContextSeed(ApplicationContext context)
{
    public async Task SeedAsync()
    {
        if (await context.Rules.AnyAsync()) return;

        context.Rules.Add(GenerateRule(
            EventType.TemperatureReading,
            null,
            "temperature > 50",
            Priority.High,
            ActionType.LogEvent,
            "Extreme temperature detected"
        ));

        context.Rules.Add(GenerateRule(
            EventType.TemperatureReading,
            "kitchen-sensor",
            "temperature > 30",
            Priority.Medium,
            ActionType.LogEvent,
            "Kitchen temperature high"
        ));

        context.Rules.Add(GenerateRule(
            EventType.TemperatureReading,
            "bedroom-sensor",
            "temperature > 22",
            Priority.Medium,
            ActionType.LogEvent,
            "Bedroom too warm for sleeping"
        ));

        context.Rules.Add(GenerateRule(
            EventType.MotionDetected,
            null,
            null,
            Priority.Low,
            ActionType.LogEvent,
            "Motion detected"
        ));

        await context.SaveChangesAsync();

    }

    private static Rule GenerateRule(EventType eventType, string? source, string? condition, Priority priority, ActionType actionType, string message)
    {
        var ruleId = Guid.NewGuid();

        var rule = new Rule
        {
            Id = ruleId,
            EventType = eventType,
            Source = source,
            Condition = condition,
            Priority = priority
        };

        rule.Actions.Add(new RuleAction
        {
            Id = Guid.NewGuid(),
            RuleId = ruleId,
            ActionType = actionType,
            Parameters = new() { ["message"] = message }
        });

        return rule;
    }
}