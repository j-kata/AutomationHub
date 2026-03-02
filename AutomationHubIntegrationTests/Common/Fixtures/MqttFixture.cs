using AutomationHub.Infrastructure.Messaging;
using AutomationHub.Infrastructure.Options;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Options;

namespace AutomationHubIntegrationTests.Common.Fixtures;

/// <summary>
/// Manages MQTT Broker test configuration.
/// For now, this assumes MQTT broker is running from docker-compose.yml.
/// Later, we can enhance this to manage containers with Testcontainers.
/// </summary>
public class MqttFixture : IAsyncLifetime
{
    private readonly IContainer _mqttContainer = new ContainerBuilder()
        .WithImage("eclipse-mosquitto:latest")
        .WithPortBinding(1883, assignRandomHostPort: true)
        .WithCommand("mosquitto", "-c", "/mosquitto-no-auth.conf")
        .WithEnvironment("ALLOW_ANONYMOUS", "true")
        .WithWaitStrategy(
            Wait.ForUnixContainer()
                .UntilPortIsAvailable(1883))
        .Build();

    public MqttOptions MqttOptions { get; private set; } = new();


    /// <summary>
    /// Get an IOptions<MqttOptions> configured for dependency injection.
    /// </summary>
    public IOptions<MqttOptions> GetMqttOptions() =>
        Options.Create(MqttOptions);


    /// <summary>
    /// Called before each test - configures MQTT settings.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await _mqttContainer.StartAsync();

            MqttOptions = new MqttOptions
            {
                Host = "localhost",
                Port = _mqttContainer.GetMappedPublicPort(1883),
                ClientId = $"test-client-{Guid.NewGuid()}",
                Topics = ["sensors/+/temperature", "sensors/+/humidity", "sensors/+/motion"],
                UseTls = false
            };
        }
        catch (Exception ex)
        {
            await DisposeAsync();
            throw new InvalidOperationException(
                "Failed to start MQTT Testcontainer. " +
                "Ensure Docker/Colima is running and accessible.",
                ex);
        }
    }

    /// <summary>
    /// Called after each test - cleanup if needed.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _mqttContainer.StopAsync();
        await _mqttContainer.DisposeAsync();
    }
}
