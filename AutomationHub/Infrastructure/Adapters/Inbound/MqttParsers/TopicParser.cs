namespace AutomationHub.Infrastructure.Adapters.Inbound.MqttParsers;

public static class TopicParser
{
    public static bool TryParseSensorTopic(string topic, out string measurement, out string source)
    {
        measurement = null!;
        source = null!;

        var segments = topic.Split('/');

        // Expected: sensors/{sensorId}/{measurement}
        if (segments.Length != 3)
            return false;

        if (segments[0] != "sensors")
            return false;

        source = segments[1];
        measurement = segments[2];

        return !string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(measurement);
    }
}
