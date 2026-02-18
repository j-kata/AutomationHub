using AutomationHub.Core.Models;

namespace AutomationHub.Core.Interfaces;

public interface IActionLogger
{
    Task Log(DomainEvent domainEvent, string message);
}