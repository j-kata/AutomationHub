using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;

namespace AutomationHub.Core.Services;

public class EventProcessor(IRuleRepository ruleRepository, IActionRegistry actionRegistry) : IEventProcessor
{
    public async void ProcessEvent(DomainEvent domainEvent)
    {
        Console.WriteLine($"Processing event {domainEvent.Id} of type {domainEvent.Type} from source {domainEvent.Source}");

        var rules = await ruleRepository.GetRulesForEvent(domainEvent.Type, domainEvent.Source);

        foreach (var rule in rules)
        {
            Console.WriteLine($"  → Found rule {rule.Id} with {rule.Actions.Count} actions");

            // TODO: Evaluate condition
            // TODO: Execute actions

            foreach (var action in rule.Actions)
            {
                if (actionRegistry.GetActionHandler(action.ActionType) is not IActionHandler actionHandler)
                    Console.WriteLine($"! No handler registered for action type {action.ActionType}");
                else
                    await actionHandler.Execute(action, domainEvent);
            }
        }
    }
}