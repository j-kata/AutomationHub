namespace AutomationHub.Infrastructure.Options;

public class MqttOptions
{
    public string ClientId { get; set; } = "AutomationHub";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string[] Topics { get; set; } = [];

    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseTls { get; set; } = false;
    public string? CertificatePath { get; set; }
}