using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using AutomationHub.Core.Services;
using AutomationHub.Infrastructure;
using AutomationHub.Infrastructure.Adapters.Outbound;
using AutomationHub.Infrastructure.Data.Repositories;
using AutomationHub.Infrastructure.Messaging;
using AutomationHubIntegrationTests.Common.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AutomationHubIntegrationTests.Integration.EndToEnd;

/// <summary>
/// End-to-end tests that wire up EventProcessor with real infrastructure:
/// real database, real SMTP server, real MQTT broker.
/// Each test proves that a domain event flows all the way through to its side effect.
/// </summary>
public class EventProcessorEndToEndTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    /// <summary>
    /// Builds a fully wired EventProcessor backed by real containers.
    /// Returns the connected MqttConnection separately so tests can subscribe to topics.
    /// </summary>
    private async Task<(EventProcessor processor, MqttConnection mqttConnection)> BuildAsync()
    {
        // Repository
        var dbContext = fixture.Database.CreateFreshDbContext();
        var repository = new RuleDbRepository(dbContext);

        // SMTP
        var smtpSender = new SmtpSender(
            fixture.Smtp.GetEmailOptions(),
            NullLogger<SmtpSender>.Instance);

        // MQTT
        var mqttConnection = new MqttConnection(
            fixture.Mqtt.GetMqttOptions(),
            NullLogger<MqttConnection>.Instance);
        await mqttConnection.ConnectAsync();

        var mqttPublisher = new MqttPublisher(mqttConnection, NullLogger<MqttPublisher>.Instance);

        // Action registry
        var registry = new ActionRegistry();
        registry.RegisterAction(ActionType.LogEvent, new ConsoleActionHandler());
        registry.RegisterAction(ActionType.SendEmail, smtpSender);
        registry.RegisterAction(ActionType.PublishMqtt, mqttPublisher);

        var processor = new EventProcessor(
            NullLogger<EventProcessor>.Instance,
            repository,
            registry);

        return (processor, mqttConnection);
    }

    /// <summary>
    /// Seeded rule: TemperatureReading from "kitchen-sensor" with temp > 30
    /// → SendEmail to kitchen@example.com with subject "Kitchen temperature high".
    /// </summary>
    [Fact]
    public async Task ProcessEvent_TemperatureReadingFromKitchen_ShouldSendEmail()
    {
        // Arrange
        using var http = new HttpClient();
        var messagesUrl = $"{fixture.Smtp.HttpApiBaseUrl}/api/messages";
        await http.DeleteAsync(messagesUrl);

        var (processor, _) = await BuildAsync();

        var domainEvent = DomainEvent.Create(
            EventType.TemperatureReading,
            "kitchen-sensor",
            new Dictionary<string, object> { ["temperature"] = 35 });

        // Act
        await processor.ProcessEvent(domainEvent);

        await Task.Delay(500); // allow smtp4dev to index the message

        // Assert
        var result = await http.GetFromJsonAsync<Smtp4DevMessageList>(messagesUrl);
        result.Should().NotBeNull();
        result!.Results.Should().ContainSingle(m => m.Subject == "Kitchen temperature high");
    }

    /// <summary>
    /// Seeded rule: MotionDetected from "living-room-sensor" (no condition)
    /// → PublishMqtt to topic "action/motion_detected".
    /// </summary>
    [Fact]
    public async Task ProcessEvent_MotionDetectedFromLivingRoom_ShouldPublishMqttMessage()
    {
        // Arrange
        var (processor, mqttConnection) = await BuildAsync();

        var receivedTopic = string.Empty;
        var messageReceived = new TaskCompletionSource<bool>();

        mqttConnection.SetMessageReceivedHandler((topic, _) =>
        {
            receivedTopic = topic;
            messageReceived.TrySetResult(true);
            return Task.CompletedTask;
        });

        await mqttConnection.SubscribeAsync(["action/motion_detected"]);

        var domainEvent = DomainEvent.Create(
            EventType.MotionDetected,
            "living-room-sensor",
            []);

        // Act
        await processor.ProcessEvent(domainEvent);

        // Assert – wait up to 3 s for the broker to deliver the message
        var received = await Task.WhenAny(messageReceived.Task, Task.Delay(3000)) == messageReceived.Task;
        received.Should().BeTrue();
        receivedTopic.Should().Be("action/motion_detected");
    }

    // DTO for smtp4dev REST API
    private sealed record Smtp4DevMessageList(
        [property: JsonPropertyName("results")] Smtp4DevMessage[] Results);

    private sealed record Smtp4DevMessage(
        [property: JsonPropertyName("subject")] string Subject);
}
