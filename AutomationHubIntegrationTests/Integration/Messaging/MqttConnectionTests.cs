using AutomationHub.Infrastructure.Messaging;
using AutomationHub.Infrastructure.Options;
using AutomationHubIntegrationTests.Common.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AutomationHubIntegrationTests.Integration.Messaging;

public class MqttConnectionTests(MqttFixture mqttFixture) : IClassFixture<MqttFixture>
{
    private MqttConnection CreateConnection() =>
        new(mqttFixture.GetMqttOptions(), NullLogger<MqttConnection>.Instance);

    [Fact]
    public async Task ConnectAsync_ValidBroker_ShouldConnect()
    {
        // Arrange
        var connection = CreateConnection();

        // Act
        await connection.ConnectAsync();

        // Assert
        connection.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectAsync_AlreadyConnected_ShouldNotThrow()
    {
        // Arrange
        var connection = CreateConnection();
        await connection.ConnectAsync();

        // Act 
        var act = () => connection.ConnectAsync();

        // Assert
        await act.Should().NotThrowAsync();
        connection.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task DisconnectAsync_WhenConnected_ShouldDisconnect()
    {
        // Arrange
        var connection = CreateConnection();
        await connection.ConnectAsync();

        // Act
        await connection.DisconnectAsync();

        // Assert
        connection.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_ShouldNotThrow()
    {
        // Arrange 
        var connection = CreateConnection();

        // Act & Assert
        var act = () => connection.DisconnectAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConnectAsync_UnreachableBroker_ShouldThrow()
    {
        // Arrange
        var badOptions = Options.Create(new MqttOptions
        {
            Host = "invalid.host.does.not.exist",
            Port = 1883,
            ClientId = "test-unreachable"
        });
        var connection = new MqttConnection(badOptions, NullLogger<MqttConnection>.Instance);

        // Act & Assert 
        var act = () => connection.ConnectAsync();
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// StartAsync is called by ASP.NET Core when the application starts.
    /// If the broker is unavailable, it must NOT crash the application.
    /// </summary>
    [Fact]
    public async Task StartAsync_UnreachableBroker_ShouldNotThrow()
    {
        // Arrange
        var badOptions = Options.Create(new MqttOptions
        {
            Host = "invalid.host.does.not.exist",
            Port = 1883,
            ClientId = "test-startup"
        });
        var connection = new MqttConnection(badOptions, NullLogger<MqttConnection>.Instance);

        // Act & Assert 
        var act = () => connection.StartAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SubscribeAsync_WhenConnected_ShouldReceivePublishedMessage()
    {
        // Arrange
        var connection = CreateConnection();
        await connection.ConnectAsync();

        var publishedTopic = "sensors/test/temperature";
        var publishedPayload = "42.0";

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

        await connection.SubscribeAsync([publishedTopic]);

        // Act
        await connection.PublishAsync(publishedTopic, publishedPayload);

        // Assert 
        var received = await Task.WhenAny(messageReceived.Task, Task.Delay(3000)) == messageReceived.Task;
        received.Should().BeTrue();
        receivedTopic.Should().Be(publishedTopic);
        receivedPayload.Should().Be(publishedPayload);
    }

    [Fact]
    public async Task SubscribeAsync_WhenNotConnected_ShouldThrow()
    {
        // Arrange 
        var connection = CreateConnection();

        // Act & Assert
        var act = () => connection.SubscribeAsync(["sensors/test/#"]);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("MQTT broker not connected.");
    }

    [Fact]
    public async Task SubscribeAsync_EmptyTopics_ShouldThrow()
    {
        // Arrange
        var connection = CreateConnection();
        await connection.ConnectAsync();

        // Act & Assert
        var act = () => connection.SubscribeAsync([]);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("At least one topic is required");
    }

    [Fact]
    public async Task PublishAsync_WhenNotConnected_ShouldThrow()
    {
        // Arrange 
        var connection = CreateConnection();

        // Act & Assert
        var act = () => connection.PublishAsync("sensors/test", "payload");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("MQTT broker not connected.");
    }

    [Fact]
    public async Task PublishAsync_EmptyTopic_ShouldThrow()
    {
        // Arrange
        var connection = CreateConnection();
        await connection.ConnectAsync();

        // Act & Assert
        var act = () => connection.PublishAsync("", "payload");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Topic cannot be null or empty");
    }
}
