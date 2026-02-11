using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Infrastructure.Adapters.Inbound.MqttParsers;

public class HumiditySensorParser : SensorBaseParser
{
    protected override string MeasurementType => "humidity";
    protected override EventType EventType => EventType.HumidityReading;
    protected override string PayloadKey => "humidity";
}