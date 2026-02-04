using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Infrastructure;

public class ActionRegistry : IActionRegistry
{
    private readonly Dictionary<ActionType, IActionHandler> _actionHandlers = new();

    public void RegisterAction(ActionType actionType, IActionHandler actionHandler)
    {
        _actionHandlers[actionType] = actionHandler;
    }

    public IActionHandler? GetActionHandler(ActionType actionType)
    {
        _actionHandlers.TryGetValue(actionType, out var handler);
        return handler;
    }
}