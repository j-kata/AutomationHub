using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using AutomationHub.Infrastructure.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AutomationHub.Infrastructure.Adapters.Outbound;

public class SmtpSender(IOptions<EmailOptions> options) : ActionHandlerBase, IEmailSender
{
    private readonly EmailOptions _options = options.Value;
    private SecureSocketOptions MapSocketOptions => _options.SocketOptions switch
    {
        SocketOptions.Auto => SecureSocketOptions.Auto,
        SocketOptions.SslOnConnect => SecureSocketOptions.SslOnConnect,
        SocketOptions.StartTls => SecureSocketOptions.StartTls,
        _ => SecureSocketOptions.None
    };
    protected override ActionType SupportedActionType => ActionType.SendEmail;

    public Task SendEmailAsync(string to, string subject, string body)
    {
        var email = new MimeMessage();

        email.From.Add(new MailboxAddress("Sender Name", _options.FromAddress));
        email.To.Add(new MailboxAddress("Receiver Name", to));

        email.Subject = subject;
        email.Body = new TextPart(MimeKit.Text.TextFormat.Plain) { Text = body };

        using (var smtp = new SmtpClient())
        {
            smtp.Connect(_options.SmtpServer, _options.Port, MapSocketOptions);
            if (!string.IsNullOrEmpty(_options.Username) && !string.IsNullOrEmpty(_options.Password))
                smtp.Authenticate(_options.Username, _options.Password);

            smtp.Send(email);
            smtp.Disconnect(true);
        }
        return Task.CompletedTask;
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