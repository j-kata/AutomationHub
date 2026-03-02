using AutomationHub.Infrastructure.Data.Contexts;
using AutomationHub.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace AutomationHubIntegrationTests.Common.Fixtures;

/// <summary>
/// Database fixture for integration tests using Testcontainers.
/// Spins up a fresh PostgreSQL container for each test collection.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private DbContextOptions<ApplicationContext>? _options;

    /// <summary>
    /// Called by xUnit before any tests run.
    /// Starts a PostgreSQL container and applies migrations (one-time setup).
    /// </summary>
    public async Task InitializeAsync()
    {
        // Create and start a PostgreSQL container
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("automation_hub_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        try
        {
            await _container.StartAsync();

            var connectionString = _container.GetConnectionString();

            // Store options for creating contexts later
            _options = new DbContextOptionsBuilder<ApplicationContext>()
                .UseNpgsql(connectionString)
                .Options;

            using var context = new ApplicationContext(_options);
            await context.Database.MigrateAsync();
            await SeedDataAsync();
        }
        catch (Exception ex)
        {
            // Clean up container if migration failed
            await DisposeAsync();
            throw new InvalidOperationException(
                "Failed to initialize Testcontainers PostgreSQL database. " +
                "Ensure Docker/Colima is running and accessible.",
                ex);
        }
    }

    /// <summary>
    /// Called by xUnit after all tests run.
    /// Stops and removes the PostgreSQL container.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Get a fresh DbContext instance and clear the database.
    /// Each test gets a clean slate.
    /// </summary>
    public ApplicationContext CreateFreshDbContext()
    {
        if (_options == null)
        {
            throw new InvalidOperationException("Container not initialized. Ensure InitializeAsync() was called.");
        }

        return new ApplicationContext(_options);
    }

    public async Task SeedDataAsync()
    {
        using var context = CreateFreshDbContext();
        context.AddRange(SeedRules.GetDefaultRules());
        await context.SaveChangesAsync();
    }
}
