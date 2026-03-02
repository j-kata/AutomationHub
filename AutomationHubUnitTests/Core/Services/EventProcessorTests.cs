using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using AutomationHub.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace AutomationHubUnitTests.Core.Services;

public class EventProcessorTests
{
    private readonly Mock<ILogger<EventProcessor>> _logger;
    private readonly Mock<IRuleRepository> _ruleRepository;
    private readonly Mock<IActionRegistry> _actionRegistry;
    private readonly EventProcessor _eventProcessor;
    private readonly DomainEvent _testEvent = DomainEvent.Create(EventType.TemperatureReading, "Sensor1", new Dictionary<string, object> { { "temperature", 25 } });

    public EventProcessorTests()
    {
        _logger = new Mock<ILogger<EventProcessor>>();
        _ruleRepository = new Mock<IRuleRepository>();
        _actionRegistry = new Mock<IActionRegistry>();
        _eventProcessor = new EventProcessor(_logger.Object, _ruleRepository.Object, _actionRegistry.Object);
    }


    [Fact]
    public async Task ProcessEvent_NoRuleFound_ShouldDoNothing()
    {
        // Arrange
        _ruleRepository.Setup(r => r.GetRulesForEvent(It.IsAny<EventType>(), It.IsAny<string>())).ReturnsAsync([]);

        var mockActionHandler = new Mock<IActionHandler>();
        _actionRegistry
            .Setup(r => r.GetActionHandler(It.IsAny<ActionType>()))
            .Returns(mockActionHandler.Object);

        // Act
        await _eventProcessor.ProcessEvent(_testEvent);

        // Assert - verify no actions were executed
        mockActionHandler
            .Verify(x => x.Execute(
                It.IsAny<RuleAction>(),
                It.IsAny<DomainEvent>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessEvent_RuleConditionMatched_ShouldExecuteActions()
    {
        // Arrange
        var rule = new Rule { Id = Guid.NewGuid(), EventType = EventType.TemperatureReading, Condition = "temperature > 20" };
        rule.Actions.Add(new RuleAction { Id = Guid.NewGuid(), ActionType = ActionType.LogEvent });


        _ruleRepository
            .Setup(r => r.GetRulesForEvent(It.IsAny<EventType>(), It.IsAny<string>()))
            .ReturnsAsync([rule]);

        var mockActionHandler = new Mock<IActionHandler>();
        _actionRegistry
            .Setup(r => r.GetActionHandler(It.Is<ActionType>(a => a == ActionType.LogEvent)))
            .Returns(mockActionHandler.Object);

        // Act
        await _eventProcessor.ProcessEvent(_testEvent);

        // Assert - verify action was executed
        mockActionHandler.Verify(x => x.Execute(It.IsAny<RuleAction>(), _testEvent), Times.Once);
    }

    [Fact]
    public async Task ProcessEvent_RuleConditionNotMatched_ShouldNotExecuteActions()
    {
        // Arrange
        var rule = new Rule { Id = Guid.NewGuid(), EventType = EventType.TemperatureReading, Condition = "temperature > 30" };
        rule.Actions.Add(new RuleAction { Id = Guid.NewGuid(), ActionType = ActionType.LogEvent });

        _ruleRepository
            .Setup(r => r.GetRulesForEvent(It.IsAny<EventType>(), It.IsAny<string>()))
            .ReturnsAsync([rule]);

        var mockActionHandler = new Mock<IActionHandler>();
        _actionRegistry
            .Setup(r => r.GetActionHandler(It.IsIn(ActionType.LogEvent)))
            .Returns(mockActionHandler.Object);

        // Act
        await _eventProcessor.ProcessEvent(_testEvent);

        // Assert - verify action was not executed
        mockActionHandler.Verify(x => x.Execute(It.IsAny<RuleAction>(), It.IsAny<DomainEvent>()), Times.Never);
    }


    [Fact]
    public async Task ProcessEvent_NoCondition_ShouldExecuteActions()
    {
        // Arrange
        var rule = new Rule { Id = Guid.NewGuid(), EventType = EventType.TemperatureReading, Condition = null };
        rule.Actions.Add(new RuleAction { Id = Guid.NewGuid(), ActionType = ActionType.LogEvent });
        _ruleRepository
            .Setup(r => r.GetRulesForEvent(It.IsAny<EventType>(), It.IsAny<string>()))
            .ReturnsAsync([rule]);

        var mockActionHandler = new Mock<IActionHandler>();
        _actionRegistry
            .Setup(r => r.GetActionHandler(It.IsIn(ActionType.LogEvent)))
            .Returns(mockActionHandler.Object);

        // Act
        await _eventProcessor.ProcessEvent(_testEvent);

        // Assert - verify action was executed
        mockActionHandler.Verify(x => x.Execute(It.IsAny<RuleAction>(), _testEvent), Times.Once);
    }

    [Fact]
    public async Task ProcessEvent_InvalidCondition_ShouldNotExecuteActions()
    {
        // Arrange
        var rule = new Rule { Id = Guid.NewGuid(), EventType = EventType.TemperatureReading, Condition = "invalid condition" };
        rule.Actions.Add(new RuleAction { Id = Guid.NewGuid(), ActionType = ActionType.LogEvent });

        _ruleRepository
            .Setup(r => r.GetRulesForEvent(It.IsAny<EventType>(), It.IsAny<string>()))
            .ReturnsAsync([rule]);

        var mockActionHandler = new Mock<IActionHandler>();
        _actionRegistry
            .Setup(r => r.GetActionHandler(It.IsIn(ActionType.LogEvent)))
            .Returns(mockActionHandler.Object);

        // Act
        await _eventProcessor.ProcessEvent(_testEvent);

        // Assert - verify action was not executed
        mockActionHandler.Verify(x => x.Execute(It.IsAny<RuleAction>(), It.IsAny<DomainEvent>()), Times.Never);
    }


    [Fact]
    public async Task ProcessEvent_MultipleRules_ShouldEvaluateAllRules()
    {
        // Arrange
        var rule1 = new Rule { Id = Guid.NewGuid(), EventType = EventType.TemperatureReading, Condition = "temperature > 20" };
        rule1.Actions.Add(new RuleAction { Id = Guid.NewGuid(), ActionType = ActionType.LogEvent });

        var rule2 = new Rule { Id = Guid.NewGuid(), EventType = EventType.TemperatureReading, Condition = "temperature > 30" };
        rule2.Actions.Add(new RuleAction { Id = Guid.NewGuid(), ActionType = ActionType.SendEmail });

        _ruleRepository
            .Setup(r => r.GetRulesForEvent(It.IsAny<EventType>(), It.IsAny<string>()))
            .ReturnsAsync([rule1, rule2]);

        var mockActionHandler = new Mock<IActionHandler>();
        _actionRegistry
            .Setup(r => r.GetActionHandler(It.IsIn(ActionType.LogEvent, ActionType.SendEmail)))
            .Returns(mockActionHandler.Object);

        // Act
        await _eventProcessor.ProcessEvent(_testEvent);

        // Assert - verify only the first rule's action was executed
        mockActionHandler.Verify(x => x.Execute(It.Is<RuleAction>(a => a.ActionType == ActionType.LogEvent), _testEvent), Times.Once);
        mockActionHandler.Verify(x => x.Execute(It.Is<RuleAction>(a => a.ActionType == ActionType.SendEmail), _testEvent), Times.Never);
    }

    [Fact]
    public async Task ProcessEvent_MultipleActions_ShouldExecuteAllActions()
    {
        // Arrange
        var rule = new Rule { Id = Guid.NewGuid(), EventType = EventType.TemperatureReading, Condition = "temperature > 20" };
        rule.Actions.Add(new RuleAction { Id = Guid.NewGuid(), ActionType = ActionType.LogEvent });
        rule.Actions.Add(new RuleAction { Id = Guid.NewGuid(), ActionType = ActionType.SendEmail });

        _ruleRepository
            .Setup(r => r.GetRulesForEvent(It.IsAny<EventType>(), It.IsAny<string>()))
            .ReturnsAsync([rule]);

        var mockActionHandler = new Mock<IActionHandler>();
        _actionRegistry
            .Setup(r => r.GetActionHandler(It.IsIn(ActionType.LogEvent, ActionType.SendEmail)))
            .Returns(mockActionHandler.Object);

        // Act
        await _eventProcessor.ProcessEvent(_testEvent);

        // Assert - verify both actions were executed
        mockActionHandler.Verify(x => x.Execute(It.Is<RuleAction>(a => a.ActionType == ActionType.LogEvent), _testEvent), Times.Once);
        mockActionHandler.Verify(x => x.Execute(It.Is<RuleAction>(a => a.ActionType == ActionType.SendEmail), _testEvent), Times.Once);
    }

    [Fact]
    public async Task ProcessEvent_ActionHandlerThrowsException_ShouldNotThrow()
    {
        // Arrange
        var rule = new Rule { Id = Guid.NewGuid(), EventType = EventType.TemperatureReading, Condition = null };
        rule.Actions.Add(new RuleAction { Id = Guid.NewGuid(), ActionType = ActionType.LogEvent });

        _ruleRepository
            .Setup(r => r.GetRulesForEvent(It.IsAny<EventType>(), It.IsAny<string>()))
            .ReturnsAsync([rule]);

        var mockActionHandler = new Mock<IActionHandler>();
        mockActionHandler
            .Setup(x => x.Execute(It.IsAny<RuleAction>(), It.IsAny<DomainEvent>()))
            .ThrowsAsync(new Exception("Action handler exception"));
        _actionRegistry
            .Setup(r => r.GetActionHandler(It.IsIn(ActionType.LogEvent)))
            .Returns(mockActionHandler.Object);

        // Act & Assert
        await _eventProcessor.ProcessEvent(_testEvent);
    }

    [Fact]
    public async Task ProcessEvent_FirstRuleConditionThrowsException_ShouldContinueProcessingOtherRules()
    {
        // Arrange
        var rule1 = new Rule { Id = Guid.NewGuid(), EventType = EventType.TemperatureReading, Condition = "temperature > 20" };
        rule1.Actions.Add(new RuleAction { Id = Guid.NewGuid(), ActionType = ActionType.LogEvent });

        var rule2 = new Rule { Id = Guid.NewGuid(), EventType = EventType.TemperatureReading, Condition = "temperature > 20" };
        rule2.Actions.Add(new RuleAction { Id = Guid.NewGuid(), ActionType = ActionType.SendEmail });

        _ruleRepository
            .Setup(r => r.GetRulesForEvent(It.IsAny<EventType>(), It.IsAny<string>()))
            .ReturnsAsync([rule1, rule2]);

        var mockActionHandler = new Mock<IActionHandler>();
        mockActionHandler
            .Setup(x => x.Execute(It.Is<RuleAction>(a => a.ActionType == ActionType.LogEvent), It.IsAny<DomainEvent>()))
            .ThrowsAsync(new Exception("Action handler exception"));
        _actionRegistry
            .Setup(r => r.GetActionHandler(It.IsIn(ActionType.LogEvent, ActionType.SendEmail)))
            .Returns(mockActionHandler.Object);

        // Act
        await _eventProcessor.ProcessEvent(_testEvent);

        // Assert - verify second rule's action was still executed despite first rule throwing
        mockActionHandler.Verify(x => x.Execute(It.Is<RuleAction>(a => a.ActionType == ActionType.SendEmail), _testEvent), Times.Once);
    }

    [Fact]
    public async Task ProcessEvent_RepoitoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        _ruleRepository
            .Setup(r => r.GetRulesForEvent(It.IsAny<EventType>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Repository exception"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _eventProcessor.ProcessEvent(_testEvent));
    }

    [Fact]
    public async Task ProcessEvent_RuleActionHandlerNotFound_ShouldNotThrow()
    {
        // Arrange
        var rule = new Rule { Id = Guid.NewGuid(), EventType = EventType.TemperatureReading, Condition = null };
        rule.Actions.Add(new RuleAction { Id = Guid.NewGuid(), ActionType = ActionType.LogEvent });

        _ruleRepository
            .Setup(r => r.GetRulesForEvent(It.IsAny<EventType>(), It.IsAny<string>()))
            .ReturnsAsync([rule]);

        _actionRegistry
            .Setup(r => r.GetActionHandler(It.IsIn(ActionType.LogEvent)))
            .Returns((IActionHandler?)null);

        // Act & Assert
        await _eventProcessor.ProcessEvent(_testEvent);
    }
}