using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Infrastructure.Adapters.Inbound.MqttParsers;

public abstract class SensorBaseParser : IMqttParser
{
    protected abstract string MeasurementType { get; }
    protected abstract EventType EventType { get; }
    protected abstract string PayloadKey { get; }

    public DomainEvent Parse(string topic, string payload)
    {
        if (TryParse(topic, payload, out var domainEvent) && domainEvent != null)
            return domainEvent;

        throw new FormatException($"Invalid topic or payload format for {GetType().Name}.");
    }

    public virtual bool TryParse(string topic, string payload, out DomainEvent? domainEvent)
    {
        domainEvent = null;

        if (!TopicParser.TryParseSensorTopic(topic, out var measurement, out var source))
            return false;

        if (measurement != MeasurementType)
            return false;

        if (!double.TryParse(payload, out var value))
            return false;

        domainEvent = DomainEvent.Create(
            type: EventType,
            source: $"mqtt/{source}",
            payload: new Dictionary<string, object>
            {
                ["topic"] = topic,
                ["source"] = source,
                [PayloadKey] = value
            }
        );

        return true;
    }
}