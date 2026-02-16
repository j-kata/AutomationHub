using System.Text.Json;
using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Infrastructure.Adapters.Outbound;

public class MqttPublisher(IMqttConnection mqttConnection) : ActionHandlerBase, IMqttPublisher
{
    protected override ActionType SupportedActionType => ActionType.PublishMqtt;

    protected override Task ExecuteAction(RuleAction action, DomainEvent domainEvent)
    {
        if (action.Parameters == null)
            throw new ArgumentException("Parameters are required for MQTT publish action.");

        if (!action.Parameters.TryGetValue("topic", out var topic) || string.IsNullOrWhiteSpace(topic?.ToString()))
            throw new ArgumentException("Invalid or missing 'topic' parameter.");

        if (!action.Parameters.TryGetValue("payload", out var payloadObj))
            throw new ArgumentException("Missing 'payload' parameter.");

        try
        {
            var payload = payloadObj switch
            {
                string s => s,
                _ => JsonSerializer.Serialize(payloadObj)
            };
            return mqttConnection.PublishAsync(topic.ToString()!, payload);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Payload must be a string or a JSON-serializable object.", ex);
        }
    }
}