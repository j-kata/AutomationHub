using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Core.Models;

public class RuleAction
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public ActionType ActionType { get; set; }
    public Dictionary<string, object>? Parameters { get; set; } = [];
}