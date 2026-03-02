namespace AutomationHubIntegrationTests.Common.Fixtures;

/// <summary>
/// Composite fixture that manages all infrastructure for integration tests.
/// This is the main fixture to use in your test classes via constructor injection.
/// 
/// Usage:
/// public class MyIntegrationTests : IAsyncLifetime
/// {
///     private readonly IntegrationTestFixture _fixture;
/// 
///     public MyIntegrationTests(IntegrationTestFixture fixture) => _fixture = fixture;
/// 
///     public Task InitializeAsync() => _fixture.InitializeAsync();
///     public Task DisposeAsync() => _fixture.DisposeAsync();
/// 
///     [Fact]
///     public async Task MyTest() { }
/// }
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    public DatabaseFixture Database { get; } = new();
    public SmtpFixture Smtp { get; } = new();
    public MqttFixture Mqtt { get; } = new();

    public async Task InitializeAsync()
    {
        // Start all containers
        await Task.WhenAll(
            Database.InitializeAsync(),
            Smtp.InitializeAsync(),
            Mqtt.InitializeAsync()
        );
    }

    public async Task DisposeAsync()
    {
        // Stop all containers
        await Task.WhenAll(
            Database.DisposeAsync(),
            Smtp.DisposeAsync(),
            Mqtt.DisposeAsync()
        );
    }
}
