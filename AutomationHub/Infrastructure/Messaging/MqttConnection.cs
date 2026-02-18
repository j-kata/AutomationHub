using System.Text;
using AutomationHub.Core.Interfaces;
using AutomationHub.Infrastructure.Options;
using Microsoft.Extensions.Options;
using MQTTnet;

namespace AutomationHub.Infrastructure.Messaging;

public class MqttConnection : IMqttConnection, IHostedService
{
    private readonly ILogger<MqttConnection> _logger;
    private readonly IMqttClient _mqttClient;
    private readonly MqttOptions _mqttOptions;
    private readonly MqttClientOptions _mqttClientOptions;
    private int _isReconnecting = 0;
    private bool IsReconnecting
    {
        get => Interlocked.CompareExchange(ref _isReconnecting, 0, 0) == 1;
        set => Interlocked.Exchange(ref _isReconnecting, value ? 1 : 0);
    }

    public bool IsConnected => _mqttClient.IsConnected;

    public MqttConnection(IOptions<MqttOptions> mqttOptions, ILogger<MqttConnection> logger)
    {
        _logger = logger;

        _mqttOptions = mqttOptions.Value;
        _mqttClientOptions = BuildMqttOptions();

        var mqttFactory = new MqttClientFactory();
        _mqttClient = mqttFactory.CreateMqttClient();
        _mqttClient.DisconnectedAsync += HandleConnectionLost;
    }

    public Task<bool> TryPingAsync(CancellationToken cancellationToken = default) =>
        _mqttClient.TryPingAsync(cancellationToken);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return ConnectAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return DisconnectAsync(cancellationToken);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;

        try
        {
            _logger.LogInformation("Attempting to connect to MQTT broker at {Host}:{Port}", _mqttOptions.Host, _mqttOptions.Port);

            await _mqttClient.ConnectAsync(_mqttClientOptions, cancellationToken);

            _logger.LogInformation("Successfully connected to MQTT broker at {Host}:{Port}", _mqttOptions.Host, _mqttOptions.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MQTT broker at {Host}:{Port}. Reason: {Reason}", _mqttOptions.Host, _mqttOptions.Port, ex.Message);
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (!IsConnected) return;

        try
        {
            _logger.LogInformation("Attempting to disconnect from MQTT broker at {Host}:{Port}", _mqttOptions.Host, _mqttOptions.Port);

            await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully disconnected from MQTT broker at {Host}:{Port}", _mqttOptions.Host, _mqttOptions.Port);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to disconnect from MQTT broker at {Host}:{Port}. Reason: {Reason}", _mqttOptions.Host, _mqttOptions.Port, ex.Message);
        }
    }

    public void SetMessageReceivedHandler(Func<string, string, Task> handler)
    {
        _mqttClient.ApplicationMessageReceivedAsync += async args =>
        {
            var topic = args.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);
            await handler(topic, payload);
        };
    }

    // TODO: check duplicate topics and handle QoS levels
    public async Task SubscribeAsync(string[] topics, CancellationToken cancellationToken)
    {
        if (!IsConnected)
            throw new InvalidOperationException("MQTT broker not connected.");

        if (topics == null || topics.Length == 0)
            throw new ArgumentException("At least one topic is required");

        try
        {
            _logger.LogInformation("Subscribing to MQTT topics: {Topics}", string.Join(", ", topics));

            var subscribeOptionsBuilder = new MqttClientSubscribeOptionsBuilder();
            foreach (var topic in topics)
                subscribeOptionsBuilder.WithTopicFilter(t => t.WithTopic(topic));

            await _mqttClient.SubscribeAsync(subscribeOptionsBuilder.Build(), cancellationToken);

            _logger.LogInformation("Successfully subscribed to MQTT topics: {Topics}", string.Join(", ", topics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to MQTT topics: {Topics}. Reason: {Reason}",
                string.Join(", ", topics), ex.Message);
            throw;
        }
    }

    public async Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken)
    {
        if (!IsConnected)
            throw new InvalidOperationException("MQTT broker not connected.");

        if (topics == null || topics.Length == 0)
            throw new ArgumentException("At least one topic is required");

        try
        {
            _logger.LogInformation("Unsubscribing from MQTT topics: {Topics}", string.Join(", ", topics));

            var unsubscribeOptionsBuilder = new MqttClientUnsubscribeOptionsBuilder();
            foreach (var topic in topics)
                unsubscribeOptionsBuilder.WithTopicFilter(topic);

            await _mqttClient.UnsubscribeAsync(unsubscribeOptionsBuilder.Build(), cancellationToken);

            _logger.LogInformation("Successfully unsubscribed from MQTT topics: {Topics}", string.Join(", ", topics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from MQTT topics: {Topics}. Reason: {Reason}",
                string.Join(", ", topics), ex.Message);
            throw;
        }
    }

    public async Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("MQTT broker not connected.");

        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be null or empty");

        try 
        {
             _logger.LogInformation("Publishing MQTT message to topic {Topic}", topic);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build();

            await _mqttClient.PublishAsync(message, cancellationToken);

            _logger.LogInformation("Successfully published MQTT message to topic {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish MQTT message to topic {Topic}. Reason: {Reason}",
                topic, ex.Message);
            throw;
        }
    }

    private MqttClientOptions BuildMqttOptions()
    {
        var optionsBuilder = new MqttClientOptionsBuilder()
           .WithTcpServer(_mqttOptions.Host, _mqttOptions.Port)
           .WithClientId(_mqttOptions.ClientId);

        if (!string.IsNullOrEmpty(_mqttOptions.Username))
        {
            optionsBuilder.WithCredentials(_mqttOptions.Username, _mqttOptions.Password);
        }

        if (_mqttOptions.UseTls)
        {
            if (string.IsNullOrEmpty(_mqttOptions.CertificatePath))
                throw new InvalidOperationException("Certificate path must be provided when UseTls is true.");

            var caChain = new System.Security.Cryptography.X509Certificates.X509Certificate2Collection();
            caChain.ImportFromPem(_mqttOptions.CertificatePath);

            optionsBuilder.WithTlsOptions(new MqttClientTlsOptionsBuilder().WithTrustChain
            (caChain).Build());
        }

        return optionsBuilder.Build();
    }

    private async Task HandleConnectionLost(MqttClientDisconnectedEventArgs e)
    {
        if (!e.ClientWasConnected || IsReconnecting)
            return;

        _logger.LogWarning("MQTT connection lost. Reason: {Reason}. Starting reconnection attempts.", e.Reason);

        IsReconnecting = true;

        try
        {
            // Exponential backoff: 1s, 2s, 4s, 8s, max 30s
            var delay = TimeSpan.FromSeconds(1);
            var maxDelay = TimeSpan.FromSeconds(30);

            while (!IsConnected)
            {
                try
                {
                    await Task.Delay(delay);
                    await _mqttClient.ConnectAsync(_mqttClientOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("MQTT reconnection attempt failed. Reason: {Reason}. Retrying in {Delay} seconds.", ex.Message, delay.TotalSeconds);

                    delay *= 2;
                    if (delay > maxDelay)
                        delay = maxDelay;
                }
            }
        }
        finally
        {
            IsReconnecting = false;
        }
    }
}