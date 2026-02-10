using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Infrastructure.Data.Seed;

public static class SeedRules
{
    public static List<Rule> GetDefaultRules() =>
        [
            CreateRule(
                eventType: EventType.TemperatureReading,
                source: null,
                condition: "temperature > 50",
                priority: Priority.High,
                actionType: ActionType.LogEvent,
                message: "Extreme temperature detected!"
            ),

            CreateRule(
                eventType: EventType.TemperatureReading,
                source: "kitchen-sensor",
                condition: "temperature > 30",
                priority: Priority.Medium,
                actionType: ActionType.LogEvent,
                message: "Kitchen temperature high"
            ),

            CreateRule(
                eventType: EventType.TemperatureReading,
                source: "bedroom-sensor",
                condition: "temperature > 22",
                priority: Priority.Medium,
                actionType: ActionType.LogEvent,
                message: "Bedroom too warm for sleeping"
            ),

            CreateRule(
                eventType: EventType.MotionDetected,
                source: null,
                condition: null,
                priority: Priority.Low,
                actionType: ActionType.LogEvent,
                message: "Motion detected"
            ),
        ];


    private static Rule CreateRule(
        EventType eventType,
        string? source,
        string? condition,
        Priority priority,
        ActionType actionType,
        string message)
    {
        var ruleId = Guid.NewGuid();

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
