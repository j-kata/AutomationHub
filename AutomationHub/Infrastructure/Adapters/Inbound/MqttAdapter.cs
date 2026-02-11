
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using AutomationHub.Infrastructure.Options;
using Microsoft.Extensions.Options;
using MQTTnet;

namespace AutomationHub.Infrastructure.Adapters.Inbound;

public class MqttAdapter : IHostedService
{
    private readonly IMqttClient _mqttClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MqttOptions _mqttOptions;
    private readonly IEnumerable<IMqttParser> _parsers;

    public MqttAdapter(IOptions<MqttOptions> mqttOptions, IServiceScopeFactory scopeFactory, IEnumerable<IMqttParser> parsers)
    {
        _mqttOptions = mqttOptions.Value;
        _scopeFactory = scopeFactory;
        _parsers = parsers;

        var mqttFactory = new MqttClientFactory();
        _mqttClient = mqttFactory.CreateMqttClient();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var mqttOptions = BuildMqttOptions();
        var subscribeOptions = BuildSubscribeOptions();

        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;

        await _mqttClient.ConnectAsync(mqttOptions, cancellationToken);
        await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
        _mqttClient.Dispose();
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

            var caChain = new X509Certificate2Collection();
            caChain.ImportFromPem(_mqttOptions.CertificatePath);

            optionsBuilder.WithTlsOptions(new MqttClientTlsOptionsBuilder().WithTrustChain
            (caChain).Build());
        }

        return optionsBuilder.Build();
    }

    private MqttClientSubscribeOptions BuildSubscribeOptions()
    {
        if (_mqttOptions.Topics == null || _mqttOptions.Topics.Length == 0)
        {
            Console.WriteLine(_mqttOptions.Topics);
            throw new InvalidOperationException(
                "No MQTT topics configured. Add 'Mqtt:Topics' to appsettings.json");
        }

        var subscribeOptionsBuilder = new MqttClientSubscribeOptionsBuilder();

        foreach (var topic in _mqttOptions.Topics)
            subscribeOptionsBuilder.WithTopicFilter(t => t.WithTopic(topic));

        return subscribeOptionsBuilder.Build();
    }

    private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs args)
    {
        var topic = args.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);

        foreach (var parser in _parsers)
        {
            if (parser.TryParse(topic, payload, out var domainEvent) && domainEvent != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var eventProcessor = scope.ServiceProvider.GetRequiredService<IEventProcessor>();
                await eventProcessor.ProcessEvent(domainEvent);
                return;
            }
        }
    }
}