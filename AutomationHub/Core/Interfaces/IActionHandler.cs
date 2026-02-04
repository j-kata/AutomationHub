using AutomationHub.Core.Models;

namespace AutomationHub.Core.Interfaces;

public interface IActionHandler
{
    Task Execute(RuleAction action, DomainEvent domainEvent);
}