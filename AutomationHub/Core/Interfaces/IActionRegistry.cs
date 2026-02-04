using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Core.Interfaces;

public interface IActionRegistry
{
    void RegisterAction(ActionType actionType, IActionHandler actionHandler);
    IActionHandler? GetActionHandler(ActionType actionType);
}