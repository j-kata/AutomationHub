using System.Text.RegularExpressions;
using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;

namespace AutomationHub.Core.Services;

public partial class EventProcessor(IRuleRepository ruleRepository, IActionRegistry actionRegistry) : IEventProcessor
{
    public async Task ProcessEvent(DomainEvent domainEvent)
    {
        Console.WriteLine($"Processing event {domainEvent.Id} of type {domainEvent.Type} from source {domainEvent.Source}");

        // TODO: split into RuleEngine if needed
        var rules = await ruleRepository.GetRulesForEvent(domainEvent.Type, domainEvent.Source);

        foreach (var rule in rules)
        {
            if (ConditionMet(rule.Condition, domainEvent))
                await ApplyRuleActions(rule, domainEvent);
        }
    }

    private static bool ConditionMet(string? condition, DomainEvent domainEvent)
    {
        if (string.IsNullOrEmpty(condition)) return true;

        var temp = MyRegex().Match(condition);
        if (temp.Success)
        {
            if (domainEvent.Payload.TryGetValue("temperature", out var t) && int.TryParse(t.ToString(), out var inValue)
                && int.TryParse(temp.Groups[1].Value, out var regValue))
            {
                return inValue > regValue;
            }
        }
        return false;
    }

    private async Task ApplyRuleActions(Rule rule, DomainEvent domainEvent)
    {
        foreach (var action in rule.Actions)
        {
            if (actionRegistry.GetActionHandler(action.ActionType) is not IActionHandler actionHandler)
                Console.WriteLine($"! No handler registered for action type {action.ActionType}");
            else
                await actionHandler.Execute(action, domainEvent);
        }
    }

    [GeneratedRegex(@"temperature > (\d+)")]
    private static partial Regex MyRegex();
}