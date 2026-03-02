using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using AutomationHub.Infrastructure.Data.Repositories;
using AutomationHub.Infrastructure.Data.Contexts;
using FluentAssertions;
using AutomationHubIntegrationTests.Common.Fixtures;
using AutomationHub.Infrastructure.Data.Seed;
using Docker.DotNet.Models;

namespace AutomationHubIntegrationTests.Integration.Data.Repositories;

/// <summary>
/// Integration tests for RuleDbRepository using real database.
public class RuleDbRepositoryTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture = fixture;

    /// <summary>
    /// Helper to get a fresh repository instance for each test.
    /// Fixture clears database before returning the context.
    /// </summary>
    private (RuleDbRepository repository, ApplicationContext context) GetFreshTestContextAsync()
    {
        // Get a fresh DbContext with cleared data from the Testcontainers fixture
        var testContext = _fixture.CreateFreshDbContextAsync();
        return (new RuleDbRepository(testContext), testContext);
    }

    [Fact]
    public async Task GetRulesForEvent_NoRulesExist_ShouldReturnEmpty()
    {
        // Arrange
        var (_repository, dbContext) = GetFreshTestContextAsync();

        // Act
        var rules = await _repository.GetRulesForEvent(EventType.HumidityReading, "Sensor1");

        // Assert
        rules.Should().BeEmpty();

        await dbContext.DisposeAsync();
    }

    [Fact]
    public async Task GetRulesForEvent_RuleExists_ShouldReturnRule()
    {
        // Arrange
        var (_repository, dbContext) = GetFreshTestContextAsync();

        // Act
        var result = await _repository.GetRulesForEvent(EventType.MotionDetected, "living-room-sensor");

        // Assert
        result.Should().HaveCount(1);
        result.First().Source.Should().Be("living-room-sensor");

        await dbContext.DisposeAsync();
    }

    [Fact]
    public async Task GetRulesForEvent_MultipleRulesExist_ShouldReturnOrderedByPriority()
    {
        // Arrange
        var (_repository, dbContext) = GetFreshTestContextAsync();

        // Act
        var result = await _repository.GetRulesForEvent(EventType.TemperatureReading, "bedroom-sensor");

        // Assert
        result.Should().HaveCount(3);
        result.First().Priority.Should().Be(Priority.High);
        result.Last().Priority.Should().Be(Priority.Low);

        await dbContext.DisposeAsync();
    }

    [Fact]
    public async Task GetRulesForEvent_WithWildcardOrNullSource_ShouldReturnRules()
    {
        // Arrange
        var (_repository, dbContext) = GetFreshTestContextAsync();

        // Act
        var result = await _repository.GetRulesForEvent(EventType.TemperatureReading, "AnySensor");

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.Source == "*" || r.Source == null).Should().BeTrue();

        await dbContext.DisposeAsync();
    }

    [Fact]
    public async Task GetRulesForEvent_RuleWithActionsExists_ShouldLoadActionsAndTransactions()
    {
        // Arrange
        var (_repository, dbContext) = GetFreshTestContextAsync();

        // Act
        var result = await _repository.GetRulesForEvent(EventType.MotionDetected, "living-room-sensor");

        // Assert
        result.Should().HaveCount(1);
        result.First().Actions.Should().HaveCount(1);
        result.First().Actions.First().ActionType.Should().Be(ActionType.PublishMqtt);
        result.First().Actions.First().Parameters.Should().ContainKey("topic").WhoseValue.ToString().Should().Be("action/motion_detected");
        result.First().Actions.First().Parameters.Should().ContainKey("payload").WhoseValue.ToString().Should().Be("Motion detected at {{timestamp}} from {{source}}");

        await dbContext.DisposeAsync();
    }
}
