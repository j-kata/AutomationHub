using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using AutomationHub.Infrastructure.Adapters.Outbound;
using AutomationHub.Infrastructure.Messaging;
using AutomationHubIntegrationTests.Common.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomationHubIntegrationTests.Integration.Adapters.Outbound;

public class MqttPublisherTests(MqttFixture mqttFixture) : IClassFixture<MqttFixture>
{
    /// <summary>
    /// Creates a connected MqttConnection and a publisher that shares it.
    /// The caller is responsible for disposing the connection.
    /// </summary>
    private async Task<(MqttConnection connection, MqttPublisher publisher)> CreateConnectedPublisherAsync()
    {
        var connection = new MqttConnection(mqttFixture.GetMqttOptions(), NullLogger<MqttConnection>.Instance);
        await connection.ConnectAsync();
        var publisher = new MqttPublisher(connection, NullLogger<MqttPublisher>.Instance);
        return (connection, publisher);
    }

    /// <summary>
    /// Publishes a message and verifies it is received via the same connection's message handler.
    /// </summary>
    [Fact]
    public async Task Execute_ValidParameters_ShouldPublishAndReceiveMessage()
    {
        // Arrange
        var (connection, publisher) = await CreateConnectedPublisherAsync();

        var publishedTopic = "sensors/kitchen/temperature";
        var publishedPayload = "25.0";

        var receivedTopic = string.Empty;
        var receivedPayload = string.Empty;
        var messageReceived = new TaskCompletionSource<bool>();

        connection.SetMessageReceivedHandler((topic, payload) =>
        {
            receivedTopic = topic;
            receivedPayload = payload;
            messageReceived.TrySetResult(true);
            return Task.CompletedTask;
        });

        await connection.SubscribeAsync(["sensors/kitchen/temperature"], CancellationToken.None);

        var action = new RuleAction
        {
            ActionType = ActionType.PublishMqtt,
            Parameters = new Dictionary<string, object>
            {
                ["topic"] = publishedTopic,
                ["payload"] = publishedPayload
            }
        };
        var domainEvent = DomainEvent.Create(EventType.TemperatureReading, "Sensor1", []);

        // Act
        await publisher.Execute(action, domainEvent);

        // Assert – wait up to 3 s for the broker to echo the message back
        var received = await Task.WhenAny(messageReceived.Task, Task.Delay(3000)) == messageReceived.Task;
        received.Should().BeTrue();
        receivedTopic.Should().Be(publishedTopic);
        receivedPayload.Should().Be(publishedPayload);
    }

    [Fact]
    public async Task Execute_NullParameters_ShouldThrowArgumentException()
    {
        // Arrange
        var (_, publisher) = await CreateConnectedPublisherAsync();

        var action = new RuleAction
        {
            ActionType = ActionType.PublishMqtt,
            Parameters = null
        };
        var domainEvent = DomainEvent.Create(EventType.TemperatureReading, "Sensor1", []);

        // Act & Assert
        var act = () => publisher.Execute(action, domainEvent);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Parameters are required for MQTT publish action.");
    }

    [Theory]
    [InlineData(null)]      // null
    [InlineData("")]        // empty string
    [InlineData("   ")]     // whitespace
    public async Task Execute_InvalidOrMissingTopic_ShouldThrowArgumentException(string? topic)
    {
        var (_, publisher) = await CreateConnectedPublisherAsync();
        Dictionary<string, object?> parameters = new() { ["topic"] = topic, ["payload"] = "25.0" };

        var action = new RuleAction
        {
            ActionType = ActionType.PublishMqtt,
            Parameters = parameters!
        };
        var domainEvent = DomainEvent.Create(EventType.TemperatureReading, "Sensor1", []);

        var act = () => publisher.Execute(action, domainEvent);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid or missing 'topic' parameter.");
    }

    [Fact]
    public async Task Execute_MissingPayloadKey_ShouldThrowArgumentException()
    {
        var (_, publisher) = await CreateConnectedPublisherAsync();

        var action = new RuleAction
        {
            ActionType = ActionType.PublishMqtt,
            Parameters = new Dictionary<string, object> { ["topic"] = "sensors/t" }
        };
        var domainEvent = DomainEvent.Create(EventType.TemperatureReading, "Sensor1", []);

        var act = () => publisher.Execute(action, domainEvent);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Missing 'payload' parameter.");
    }

    [Theory]
    [InlineData("")]            // empty string
    [InlineData("   ")]         // whitespace
    public async Task Execute_InvalidPayloadValue_ShouldThrowArgumentException(string payload)
    {
        var (_, publisher) = await CreateConnectedPublisherAsync();

        var action = new RuleAction
        {
            ActionType = ActionType.PublishMqtt,
            Parameters = new Dictionary<string, object> { ["topic"] = "sensors/t", ["payload"] = payload }
        };
        var domainEvent = DomainEvent.Create(EventType.TemperatureReading, "Sensor1", []);

        var act = () => publisher.Execute(action, domainEvent);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid or empty 'payload' parameter.");
    }
}