namespace AutomationHub.Infrastructure.Options;

public enum SocketOptions
{
    None,
    Auto,
    SslOnConnect,
    StartTls
}

public class EmailOptions
{
    public string SmtpServer { get; set; } = "";
    public int Port { get; set; } = 25;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "";
    public SocketOptions SocketOptions = SocketOptions.None;
}
