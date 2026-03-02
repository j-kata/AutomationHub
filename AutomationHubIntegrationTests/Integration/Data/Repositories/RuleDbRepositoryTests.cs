using AutomationHub.Core.Models.Constants;
using AutomationHub.Infrastructure.Data.Repositories;
using AutomationHub.Infrastructure.Data.Contexts;
using FluentAssertions;
using AutomationHubIntegrationTests.Common.Fixtures;

namespace AutomationHubIntegrationTests.Integration.Data.Repositories;

/// <summary>
/// Integration tests for RuleDbRepository using real database.
public class RuleDbRepositoryTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetRulesForEvent_NoRulesExist_ShouldReturnEmpty()
    {
        // Arrange
        await using var dbContext = _fixture.CreateFreshDbContext();
        var repository = new RuleDbRepository(dbContext);

        // Act
        var rules = await repository.GetRulesForEvent(EventType.HumidityReading, "Sensor1");

        // Assert
        rules.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRulesForEvent_RuleExists_ShouldReturnRule()
    {
        // Arrange
        await using var dbContext = _fixture.CreateFreshDbContext();
        var repository = new RuleDbRepository(dbContext);

        // Act
        var result = await repository.GetRulesForEvent(EventType.MotionDetected, "living-room-sensor");

        // Assert
        result.Should().HaveCount(1);
        result.First().Source.Should().Be("living-room-sensor");
    }

    [Fact]
    public async Task GetRulesForEvent_MultipleRulesExist_ShouldReturnOrderedByPriority()
    {
        // Arrange
        await using var dbContext = _fixture.CreateFreshDbContext();
        var repository = new RuleDbRepository(dbContext);

        // Act
        var result = await repository.GetRulesForEvent(EventType.TemperatureReading, "bedroom-sensor");

        // Assert
        result.Should().HaveCount(3);
        result.First().Priority.Should().Be(Priority.High);
        result.Last().Priority.Should().Be(Priority.Low);
    }

    [Fact]
    public async Task GetRulesForEvent_WithWildcardOrNullSource_ShouldReturnRules()
    {
        // Arrange
        await using var dbContext = _fixture.CreateFreshDbContext();
        var repository = new RuleDbRepository(dbContext);

        // Act
        var result = await repository.GetRulesForEvent(EventType.TemperatureReading, "AnySensor");

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.Source == "*" || r.Source == null).Should().BeTrue();
    }

    [Fact]
    public async Task GetRulesForEvent_RuleWithActionsExists_ShouldLoadActions()
    {
        // Arrange
        await using var dbContext = _fixture.CreateFreshDbContext();
        var repository = new RuleDbRepository(dbContext);

        // Act
        var result = await repository.GetRulesForEvent(EventType.MotionDetected, "living-room-sensor");

        // Assert
        result.Should().HaveCount(1);
        result.First().Actions.Should().HaveCount(1);
        result.First().Actions.First().ActionType.Should().Be(ActionType.PublishMqtt);
        result.First().Actions.First().Parameters.Should().ContainKey("topic").WhoseValue.ToString().Should().Be("action/motion_detected");
        result.First().Actions.First().Parameters.Should().ContainKey("payload").WhoseValue.ToString().Should().Be("Motion detected at {{timestamp}} from {{source}}");
    }
}
