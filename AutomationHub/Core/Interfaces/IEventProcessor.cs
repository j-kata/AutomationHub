using AutomationHub.Core.Models;

namespace AutomationHub.Core.Interfaces;

public interface IEventProcessor
{
    Task ProcessEvent(DomainEvent domainEvent);
}