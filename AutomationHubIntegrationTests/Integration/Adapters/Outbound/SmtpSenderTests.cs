using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using AutomationHub.Infrastructure.Adapters.Outbound;
using AutomationHub.Infrastructure.Options;
using AutomationHubIntegrationTests.Common.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AutomationHubIntegrationTests.Integration.Adapters.Outbound;

public class SmtpSenderTests(SmtpFixture smtpFixture) : IClassFixture<SmtpFixture>
{
    private readonly SmtpFixture _smtpFixture = smtpFixture;

    private string Smtp4DevMessagesUrl => $"{_smtpFixture.HttpApiBaseUrl}/api/messages";

    private SmtpSender CreateSender() =>
        new(_smtpFixture.GetEmailOptions(), NullLogger<SmtpSender>.Instance);

    /// <summary>
    /// Sends an email and verifies it was received via the SMTP4Dev REST API. 
    /// </summary>
    [Fact]
    public async Task SendEmailAsync_ValidParameters_ShouldDeliverEmailToSmtp4Dev()
    {
        // Arrange
        using var http = new HttpClient();
        await http.DeleteAsync(Smtp4DevMessagesUrl); // clear inbox before test

        var sender = CreateSender();
        var subject = $"Integration test {Guid.NewGuid()}";

        // Act
        await sender.SendEmailAsync("recipient@test.local", subject, "Hello");

        await Task.Delay(500);

        // Assert 
        var result = await http.GetFromJsonAsync<Smtp4DevMessageList>(Smtp4DevMessagesUrl);

        result.Should().NotBeNull();
        result.Results.Should().ContainSingle(m => m.Subject == subject);
    }

    /// <summary>
    /// When the SMTP server is unreachable, the exception propagates so the caller
    /// (EventProcessor) can handle and log it with full context.
    /// </summary>
    [Fact]
    public async Task SendEmailAsync_UnreachableSmtpServer_ShouldThrow()
    {
        // Arrange – point at a host that will never answer
        var badOptions = Options.Create(new EmailOptions
        {
            SmtpServer = "invalid.host.does.not.exist",
            Port = 2525,
            FromAddress = "test@test.local",
            SocketOptions = SocketOptions.None
        });
        var sender = new SmtpSender(badOptions, NullLogger<SmtpSender>.Instance);

        // Act & Assert
        var act = () => sender.SendEmailAsync("to@test.local", "subject", "body");
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// ExecuteAction must throw when Parameters is null.
    /// </summary>
    [Fact]
    public async Task ExecuteAction_NullParameters_ShouldThrowArgumentException()
    {
        // Arrange
        var sender = CreateSender();
        var action = new RuleAction { ActionType = ActionType.SendEmail, Parameters = null };
        var domainEvent = DomainEvent.Create(EventType.TemperatureReading, "Sensor1", []);

        // Act & Assert
        var res = () => sender.Execute(action, domainEvent);
        await res.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Parameters are required for SendEmail action.");
    }

    /// <summary>
    /// ExecuteAction must throw when any required parameter is absent or empty.
    /// </summary>
    [Theory]
    [InlineData("to", "", "s", "b")]                    // to is empty
    [InlineData("subject", "to@test.local", "", "b")]   // subject is empty
    [InlineData("body", "to@test.local", "s", "")]      // body is empty
    public async Task ExecuteAction_MissingOrEmptyParameter_ShouldThrowArgumentException(string param, string to, string subject, string body)
    {
        // Arrange
        var sender = CreateSender();
        var action = new RuleAction
        {
            ActionType = ActionType.SendEmail,
            Parameters = new Dictionary<string, object>
            {
                ["to"] = to,
                ["subject"] = subject,
                ["body"] = body
            }
        };
        var domainEvent = DomainEvent.Create(EventType.TemperatureReading, "Sensor1", []);

        // Act & Assert
        var res = () => sender.Execute(action, domainEvent);
        await res.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Invalid or missing '{param}' parameter.");
    }


    /// <summary>
    /// When credentials are configured, Authenticate must be called.
    /// smtp4dev accepts any credentials so this verifies the email still arrives.
    /// </summary>
    [Fact]
    public async Task SendEmailAsync_WithCredentials_ShouldAuthenticateAndDeliver()
    {
        // Arrange
        using var http = new HttpClient();
        await http.DeleteAsync(Smtp4DevMessagesUrl);

        var optionsWithAuth = Options.Create(new EmailOptions
        {
            SmtpServer = _smtpFixture.EmailOptions.SmtpServer,
            Port = _smtpFixture.EmailOptions.Port,
            FromAddress = _smtpFixture.EmailOptions.FromAddress,
            SocketOptions = SocketOptions.None,
            Username = "testuser",
            Password = "testpass"
        });
        var sender = new SmtpSender(optionsWithAuth, NullLogger<SmtpSender>.Instance);
        var subject = $"Auth test {Guid.NewGuid()}";

        // Act
        await sender.SendEmailAsync("recipient@test.local", subject, "Auth test body");

        await Task.Delay(500);

        // Assert
        var result = await http.GetFromJsonAsync<Smtp4DevMessageList>(Smtp4DevMessagesUrl);
        result.Should().NotBeNull();
        result!.Results.Should().ContainSingle(m => m.Subject == subject);
    }

    // DTO types that mirror the SMTP4Dev paged-response format
    private sealed record Smtp4DevMessageList(
        [property: JsonPropertyName("results")] Smtp4DevMessage[] Results,
        [property: JsonPropertyName("totalCount")] int TotalCount);

    private sealed record Smtp4DevMessage(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("subject")] string Subject);
}