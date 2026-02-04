using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Infrastructure.Adapters.Outbound;

public abstract class ActionHandlerBase : IActionHandler
{
    protected abstract ActionType SupportedActionType { get; }

    public Task Execute(RuleAction action, DomainEvent domainEvent)
    {
        if (action.ActionType != SupportedActionType)
            throw new InvalidOperationException($"{GetType().Name} cannot execute action of type {action.ActionType}");

        return ExecuteAction(action, domainEvent);
    }

    protected abstract Task ExecuteAction(RuleAction action, DomainEvent domainEvent);
}