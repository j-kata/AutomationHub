using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Core.Models;

public class Rule
{
    public Guid Id { get; set; }
    public EventType EventType { get; set; }
    public string? Source { get; set; } = string.Empty;
    public string? Condition { get; set; } = string.Empty;
    public Priority Priority { get; set; } = Priority.Low;
    public ICollection<RuleAction> Actions { get; set; } = [];
}