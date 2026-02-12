using AutomationHub.Core.Interfaces;
using AutomationHub.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace AutomationHub.Infrastructure.Adapters.Inbound;

public class MqttSubscriber(IMqttConnection mqttConnection, IOptions<MqttOptions> mqttOptions, IServiceScopeFactory scopeFactory, IEnumerable<IMqttParser> parsers) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        mqttConnection.SetMessageReceivedHandler(OnMessageReceived);
        return mqttConnection.SubscribeAsync(mqttOptions.Value.Topics, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return mqttConnection.UnsubscribeAsync(mqttOptions.Value.Topics, cancellationToken);
    }

    private async Task OnMessageReceived(string topic, string payload)
    {
        foreach (var parser in parsers)
        {
            if (parser.TryParse(topic, payload, out var domainEvent) && domainEvent != null)
            {
                using var scope = scopeFactory.CreateScope();
                var eventProcessor = scope.ServiceProvider.GetRequiredService<IEventProcessor>();
                await eventProcessor.ProcessEvent(domainEvent);
                return;
            }

            // TODO: Add logging for unhandled messages
        }
    }
}