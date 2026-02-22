using AutomationHub.Infrastructure.Messaging;
using AutomationHub.Infrastructure.Options;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AutomationHubTests.Infrastructure.Messaging;

public class MqttConnectionTests
{
    private readonly MqttConnection _mqttConnection;
    private readonly Mock<ILogger<MqttConnection>> _logger;

    public MqttConnectionTests()
    {
        var mqttOptions = Options.Create(new MqttOptions
        {
            Host = "localhost",
            Port = 1883,
            ClientId = "test-client"
        });
        _logger = new Mock<ILogger<MqttConnection>>();
        _mqttConnection = new MqttConnection(mqttOptions, _logger.Object);
    }

    [Fact]
    public async Task PublishAsync_NullTopic_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _mqttConnection.PublishAsync(null!, "payload"));
    }

    [Fact]
    public async Task PublishAsync_EmptyTopic_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _mqttConnection.PublishAsync("", "payload"));
    }

    [Fact]
    public async Task SubscribeAsync_NullTopics_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _mqttConnection.SubscribeAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsync_EmptyTopics_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _mqttConnection.SubscribeAsync([], CancellationToken.None));
    }

    [Fact]
    public async Task UnsubscribeAsync_NullTopics_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _mqttConnection.UnsubscribeAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UnsubscribeAsync_EmptyTopics_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _mqttConnection.UnsubscribeAsync([], CancellationToken.None));
    }

    [Fact]
    public void IsConnected_ShouldReturnFalseInitially()
    {
        // Assert - MqttClient starts disconnected
        _mqttConnection.IsConnected.Should().BeFalse();
    }
}