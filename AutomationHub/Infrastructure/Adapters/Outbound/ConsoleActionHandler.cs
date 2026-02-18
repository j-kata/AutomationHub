using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Infrastructure.Adapters.Outbound;

public class ConsoleActionHandler : ActionHandlerBase, IActionLogger
{
    protected override ActionType SupportedActionType => ActionType.LogEvent;

    public Task Log(DomainEvent domainEvent, string message)
    {
        Console.WriteLine($"[{DateTime.UtcNow:O}] {domainEvent.Id}[{domainEvent.Type}] - {message}");
        return Task.CompletedTask;
    }

    protected override Task ExecuteAction(RuleAction action, DomainEvent domainEvent)
    {
        var message = action.Parameters?["message"]?.ToString() ?? "No message provided";

        return Log(domainEvent, message);
    }
}