using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Infrastructure.Adapters.Inbound;
using AutomationHub.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AutomationHubUnitTests.Infrastructure.Adapters.Inbound;

public class MqttSubscriberTests
{
    private readonly Mock<IMqttConnection> _mqttConnection;
    private readonly Mock<ILogger<MqttSubscriber>> _logger;
    private readonly Mock<IOptions<MqttOptions>> _mqttOptions;
    private readonly Mock<IServiceScopeFactory> _scopeFactory;
    private readonly IEnumerable<IMqttParser> _parsers = [];
    private readonly string _testTopic = "sensors/livingroom/temperature";
    private readonly MqttSubscriber _subscriber;

    public MqttSubscriberTests()
    {
        _mqttConnection = new Mock<IMqttConnection>();
        _logger = new Mock<ILogger<MqttSubscriber>>();
        _mqttOptions = new Mock<IOptions<MqttOptions>>();
        _mqttOptions.SetupGet(m => m.Value).Returns(new MqttOptions { Topics = [_testTopic] });
        _scopeFactory = new Mock<IServiceScopeFactory>();
        _subscriber = new MqttSubscriber(_mqttConnection.Object, _logger.Object, _mqttOptions.Object, _scopeFactory.Object, _parsers);
    }

    [Fact]
    public async Task StartAsync_ShouldSubscribeToTopics()
    {
        // Act
        await _subscriber.StartAsync(CancellationToken.None);

        // Assert
        _mqttConnection.Verify(m => m.SubscribeAsync(It.Is<string[]>(t => t.SequenceEqual(new[] { _testTopic })), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ThrowsException_ShouldNotThrow()
    {
        // Arrange
        _mqttConnection.Setup(m => m.SubscribeAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());

        // Act & Assert
        await _subscriber.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_SetsUpMessageHandler()
    {
        // Arrange & Act
        await _subscriber.StartAsync(CancellationToken.None);

        // Assert - verify SetMessageReceivedHandler was called
        _mqttConnection.Verify(
            m => m.SetMessageReceivedHandler(It.IsAny<Func<string, string, Task>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldUnsubscribeFromTopics()
    {
        // Act
        await _subscriber.StopAsync(CancellationToken.None);

        // Assert
        _mqttConnection.Verify(m => m.UnsubscribeAsync(It.Is<string[]>(t => t.SequenceEqual(new[] { _testTopic })), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ThrowsException_ShouldNotThrow()
    {
        // Arrange
        _mqttConnection.Setup(m => m.UnsubscribeAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());

        // Act & Assert
        await _subscriber.StopAsync(CancellationToken.None);
    }

}