namespace AutomationHub.Core.Interfaces;

public interface IMqttConnection
{
    Task<bool> TryPingAsync(CancellationToken cancellationToken = default);
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }
    Task SubscribeAsync(string[] topics, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default);
    Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default);
    void SetMessageReceivedHandler(Func<string, string, Task> handler);
}