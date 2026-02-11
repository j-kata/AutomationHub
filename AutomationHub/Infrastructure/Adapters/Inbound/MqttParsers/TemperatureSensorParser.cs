using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Infrastructure.Adapters.Inbound.MqttParsers;

public class TemperatureSensorParser : SensorBaseParser
{
    protected override string MeasurementType => "temperature";
    protected override EventType EventType => EventType.TemperatureReading;
    protected override string PayloadKey => "temperature";
}