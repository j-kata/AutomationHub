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
                parameters: new Dictionary<string, object> { ["message"] = "Extreme temperature detected!" }
            ),

            CreateRule(
                eventType: EventType.TemperatureReading,
                source: "*",
                condition: "temperature > 40",
                priority: Priority.Medium,
                actionType: ActionType.LogEvent,
                parameters: new Dictionary<string, object> { ["message"] = "Extreme temperature detected!" }
            ),

            CreateRule(
                eventType: EventType.TemperatureReading,
                source: "kitchen-sensor",
                condition: "temperature > 30",
                priority: Priority.Medium,
                actionType: ActionType.SendEmail,
                parameters: new Dictionary<string, object> {
                    ["to"] = "kitchen@example.com",
                    ["subject"] = "Kitchen temperature high",
                    ["body"] = "The temperature in the kitchen has exceeded the threshold."
                }
            ),

            CreateRule(
                eventType: EventType.TemperatureReading,
                source: "bedroom-sensor",
                condition: "temperature > 22",
                priority: Priority.Low,
                actionType: ActionType.LogEvent,
                parameters: new Dictionary<string, object> { ["message"] = "Bedroom too warm for sleeping" }
            ),

            CreateRule(
                eventType: EventType.MotionDetected,
                source: "living-room-sensor",
                condition: null,
                priority: Priority.Low,
                actionType: ActionType.PublishMqtt,
                parameters: new Dictionary<string, object> {
                    ["topic"] = "action/motion_detected",
                    ["payload"] = "Motion detected at {{timestamp}} from {{source}}"
                }
            ),
        ];


    private static Rule CreateRule(
        EventType eventType,
        string? source,
        string? condition,
        Priority priority,
        ActionType actionType,
        Dictionary<string, object>? parameters = null)
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
                    Parameters = parameters
                }
            ]
        };
    }
}
