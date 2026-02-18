using System.Text.RegularExpressions;
using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;

namespace AutomationHub.Core.Services;

public partial class EventProcessor(ILogger<EventProcessor> logger, IRuleRepository ruleRepository, IActionRegistry actionRegistry) : IEventProcessor
{
    public async Task ProcessEvent(DomainEvent domainEvent)
    {
        logger.LogInformation("Processing event {EventId}", domainEvent.Id);

        // TODO: split into RuleEngine if needed
        var rules = await ruleRepository.GetRulesForEvent(domainEvent.Type, domainEvent.Source);

        if (!rules.Any())
        {
            logger.LogInformation("No matching rules found for event {EventId}", domainEvent.Id);
            return;
        }

        foreach (var rule in rules)
        {
            try
            {
                if (ConditionMet(rule.Condition, domainEvent))
                {
                    logger.LogInformation("Rule condition met for rule {RuleId}", rule.Id);
                    await ApplyRuleActions(rule, domainEvent);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing rule {RuleId} for event {EventId}", rule.Id, domainEvent.Id);
            }
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
            if (actionRegistry.GetActionHandler(action.ActionType) is IActionHandler actionHandler)
            {
                try
                {
                    logger.LogInformation("Executing action {ActionId} of type {ActionType} for rule {RuleId}", action.Id, action.ActionType, rule.Id);
                    await actionHandler.Execute(action, domainEvent);
                    logger.LogInformation("Action {ActionId} succeeded", action.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error executing action {ActionId} of type {ActionType} for rule {RuleId}", action.Id, action.ActionType, rule.Id);
                }
            }
            else
            {
                logger.LogWarning("No handler registered for action type {ActionType}", action.ActionType);
            }

        }
    }

    [GeneratedRegex(@"temperature > (\d+)")]
    private static partial Regex MyRegex();
}