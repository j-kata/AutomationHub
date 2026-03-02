using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using AutomationHub.Infrastructure.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AutomationHub.Infrastructure.Adapters.Outbound;

public class SmtpSender(IOptions<EmailOptions> options, ILogger<SmtpSender> logger) : ActionHandlerBase, IEmailSender
{
    private readonly EmailOptions _options = options.Value;
    private readonly ILogger<SmtpSender> _logger = logger;
    private SecureSocketOptions MapSocketOptions => _options.SocketOptions switch
    {
        SocketOptions.Auto => SecureSocketOptions.Auto,
        SocketOptions.SslOnConnect => SecureSocketOptions.SslOnConnect,
        SocketOptions.StartTls => SecureSocketOptions.StartTls,
        _ => SecureSocketOptions.None
    };
    protected override ActionType SupportedActionType => ActionType.SendEmail;

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("Preparing to send email to {To} with subject '{Subject}'", to, subject);
        var email = new MimeMessage();

        email.From.Add(new MailboxAddress("Sender Name", _options.FromAddress));
        email.To.Add(new MailboxAddress("Receiver Name", to));

        email.Subject = subject;
        email.Body = new TextPart(MimeKit.Text.TextFormat.Plain) { Text = body };

        using var smtp = new SmtpClient();

        _logger.LogInformation("Connecting to SMTP server {SmtpServer}:{Port} with socket options {SocketOptions}", _options.SmtpServer, _options.Port, _options.SocketOptions);

        await smtp.ConnectAsync(_options.SmtpServer, _options.Port, MapSocketOptions);
        if (!string.IsNullOrEmpty(_options.Username) && !string.IsNullOrEmpty(_options.Password))
            await smtp.AuthenticateAsync(_options.Username, _options.Password);

        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);

        _logger.LogInformation("Email sent successfully to {To}", to);
    }

    protected override Task ExecuteAction(RuleAction action, DomainEvent domainEvent)
    {
        if (action.Parameters == null)
            throw new ArgumentException("Parameters are required for SendEmail action.");

        if (!action.Parameters.TryGetValue("to", out var to) || string.IsNullOrWhiteSpace(to?.ToString()))
            throw new ArgumentException("Invalid or missing 'to' parameter.");

        if (!action.Parameters.TryGetValue("subject", out var subject) || string.IsNullOrWhiteSpace(subject?.ToString()))
            throw new ArgumentException("Invalid or missing 'subject' parameter.");

        if (!action.Parameters.TryGetValue("body", out var body) || string.IsNullOrWhiteSpace(body?.ToString()))
            throw new ArgumentException("Invalid or missing 'body' parameter.");

        return SendEmailAsync(to.ToString()!, subject.ToString()!, body.ToString()!);
    }
}