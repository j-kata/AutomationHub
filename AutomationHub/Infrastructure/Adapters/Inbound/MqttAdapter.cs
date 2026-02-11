
using System.Text;
using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using MQTTnet;

namespace AutomationHub.Infrastructure.Adapters.Inbound;

public class MqttAdapter : IHostedService
{
    private readonly IMqttClient _mqttClient;
    private readonly IServiceScopeFactory _scopeFactory;

    public MqttAdapter(IServiceScopeFactory scopeFactory)        
    {
        _scopeFactory = scopeFactory;
        
        var mqttFactory = new MqttClientFactory();
        _mqttClient = mqttFactory.CreateMqttClient();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;

        await _mqttClient.ConnectAsync(mqttOptions, cancellationToken);
        await _mqttClient.SubscribeAsync(
            new MqttTopicFilterBuilder()
            .WithTopic("sensors/+/temperature").Build(), cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
        _mqttClient.Dispose();
    }

    private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs args)
    {
        var topic = args.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);

        var parts = topic.Split('/');
        if (parts.Length == 3 && parts[0] == "sensors")
        {
            var sensorId = parts[1];
            var measurement = parts[2];

            var domainEvent = DomainEvent.Create(
                type: EventType.TemperatureReading,
                source: $"mqtt/{sensorId}",
                payload: new Dictionary<string, object>
                {
                    ["temperature"] = int.Parse(payload),
                    ["topic"] = topic
                }
            );

            // Create a scope to get scoped services
            using var scope = _scopeFactory.CreateScope();
            var eventProcessor = scope.ServiceProvider.GetRequiredService<IEventProcessor>();
            await eventProcessor.ProcessEvent(domainEvent);
        }
    }
}