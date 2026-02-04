using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Infrastructure.Adapters.Outbound;

public class ConsoleLogger : ActionHandlerBase, Core.Interfaces.ILogger
{
    protected override ActionType SupportedActionType => ActionType.LogEvent;

    protected override Task ExecuteAction(RuleAction action, DomainEvent domainEvent)
    {
        Console.WriteLine(
            "[LogEvent] Event Type: {0}, Source: {1}, Timestamp: {2}, Message: {3}",
            domainEvent.Type,
            domainEvent.Source,
            domainEvent.CreatedAt,
            action.Parameters?["message"]);

        return Task.CompletedTask;
    }
}