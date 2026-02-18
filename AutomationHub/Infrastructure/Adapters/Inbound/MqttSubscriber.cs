using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace AutomationHub.Infrastructure.Adapters.Inbound;

public class MqttSubscriber(IMqttConnection mqttConnection, ILogger<MqttSubscriber> logger, IOptions<MqttOptions> mqttOptions, IServiceScopeFactory scopeFactory, IEnumerable<IMqttParser> parsers) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            mqttConnection.SetMessageReceivedHandler(OnMessageReceived);
            logger.LogInformation("Starting MQTT subscriber and subscribing to topics.");
            await mqttConnection.SubscribeAsync(mqttOptions.Value.Topics, cancellationToken);
            logger.LogInformation("MQTT subscriber started and subscribed to topics successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start MQTT subscriber and subscribe to topics. Reason: {Reason}", ex.Message);
            // Do not throw for now to allow the application to start even if MQTT connection fails. Consider retry logic or health checks for better resilience.
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Stopping MQTT subscriber and unsubscribing from topics.");
            await mqttConnection.UnsubscribeAsync(mqttOptions.Value.Topics, cancellationToken);
            logger.LogInformation("MQTT subscriber stopped and unsubscribed from topics successfully.");
        } 
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to stop MQTT subscriber and unsubscribe from topics. Reason: {Reason}", ex.Message);
            // Do not throw for now to allow the application to stop gracefully even if MQTT disconnection fails.
        }
    }

    private async Task OnMessageReceived(string topic, string payload)
    {
        logger.LogInformation("Received MQTT message on topic {Topic}", topic);

        try
        {
            DomainEvent? domainEvent = null;
            var parser = parsers.FirstOrDefault(p => p.TryParse(topic, payload, out domainEvent));
            if (parser is null)
            {
                logger.LogWarning("No parser found for MQTT message on topic {Topic}", topic);
                return;
            }

            if (domainEvent is null)
            {
                logger.LogWarning("Parser {Parser} failed to parse MQTT message on topic {Topic}", parser.GetType().Name, topic);
                return;
            }

            logger.LogInformation("Parsed MQTT message into domain event {EventId} of type {EventType}", domainEvent.Id, domainEvent.Type);

            using var scope = scopeFactory.CreateScope();
            var eventProcessor = scope.ServiceProvider.GetRequiredService<IEventProcessor>();

            await eventProcessor.ProcessEvent(domainEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing MQTT message on topic {Topic}", topic);
        }
    }
}