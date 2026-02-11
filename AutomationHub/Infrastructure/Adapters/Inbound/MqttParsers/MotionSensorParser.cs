using System.Text.Json;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Infrastructure.Adapters.Inbound.MqttParsers;

record MotionData(string SensorId, bool Detected, string Location, DateTime Timestamp);


public class MotionSensorParser : SensorBaseParser
{
    protected override string MeasurementType => "motion";
    protected override EventType EventType => EventType.MotionDetected;
    protected override string PayloadKey => ""; // not used

    // Override since motion sensor uses JSON, not simple numeric payload
    public override bool TryParse(string topic, string payload, out DomainEvent? domainEvent)
    {
        domainEvent = null;

        if (!TopicParser.TryParseSensorTopic(topic, out var measurement, out var source))
            return false;

        if (measurement != MeasurementType)
            return false;

        try
        {
            var data = JsonSerializer.Deserialize<MotionData>(payload);
            if (data == null)
                return false;

            domainEvent = DomainEvent.Create(
                type: EventType,
                source: $"mqtt/{source}",
                payload: new Dictionary<string, object>
                {
                    ["topic"] = topic,
                    ["source"] = source,
                    ["sensorId"] = data.SensorId,
                    ["detected"] = data.Detected,
                    ["location"] = data.Location,
                    ["timestamp"] = data.Timestamp
                }
            );

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}