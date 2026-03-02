using System.Text.Json;
using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using Microsoft.Extensions.Logging;

namespace AutomationHub.Infrastructure.Adapters.Outbound;

public class MqttPublisher(IMqttConnection mqttConnection, ILogger<MqttPublisher> logger) : ActionHandlerBase, IMqttPublisher
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

        if (payloadObj is null || (payloadObj is string s && string.IsNullOrWhiteSpace(s)))
            throw new ArgumentException("Invalid or empty 'payload' parameter.");

        try
        {
            var payload = payloadObj switch
            {
                string str => str,
                _ => JsonSerializer.Serialize(payloadObj)
            };

            logger.LogInformation("Publishing MQTT message to topic {Topic} for event {EventId}", topic, domainEvent.Id);
            return mqttConnection.PublishAsync(topic.ToString()!, payload);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Payload must be a string or a JSON-serializable object.", ex);
        }
    }
}