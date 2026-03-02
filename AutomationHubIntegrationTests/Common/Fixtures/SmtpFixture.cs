using AutomationHub.Infrastructure.Options;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Options;

namespace AutomationHubIntegrationTests.Common.Fixtures;

/// <summary>
/// Starts a real smtp4dev container for each test collection using Testcontainers.
/// The container is created fresh on every test run and torn down afterwards,
/// so there is no dependency on docker-compose being up.
/// </summary>
public class SmtpFixture : IAsyncLifetime
{
    private const int SmtpPort = 25;
    private const int HttpPort = 80;

    private readonly IContainer _container = new ContainerBuilder()
        .WithImage("rnwood/smtp4dev:latest")
        .WithPortBinding(SmtpPort, assignRandomHostPort: true)
        .WithPortBinding(HttpPort, assignRandomHostPort: true)
        .WithWaitStrategy(
            Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r.ForPort(HttpPort)))
        .Build();

    public EmailOptions EmailOptions { get; private set; } = new();

    /// <summary>Returns IOptions wired to this container's SMTP port.</summary>
    public IOptions<EmailOptions> GetEmailOptions() => Options.Create(EmailOptions);

    /// <summary>Base URL of the smtp4dev REST API, e.g. http://localhost:32768</summary>
    public string HttpApiBaseUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Called by xUnit before any tests run.
    /// Starts the smtp4dev container and configures SMTP settings.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();

            var smtpHostPort = _container.GetMappedPublicPort(SmtpPort);
            var httpHostPort = _container.GetMappedPublicPort(HttpPort);

            HttpApiBaseUrl = $"http://localhost:{httpHostPort}";

            EmailOptions = new EmailOptions
            {
                SmtpServer = "localhost",
                Port = smtpHostPort,
                FromAddress = "test@automation-hub.local",
                SocketOptions = SocketOptions.None,
                Username = null,
                Password = null
            };
        }
        catch (Exception ex)
        {
            await DisposeAsync();
            throw new InvalidOperationException(
                "Failed to start smtp4dev Testcontainer. " +
                "Ensure Docker/Colima is running and accessible.",
                ex);
        }
    }

    /// <summary>
    /// Called by xUnit after all tests run. Stops and removes the container.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }
}
