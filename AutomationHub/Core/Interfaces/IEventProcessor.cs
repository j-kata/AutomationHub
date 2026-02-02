using AutomationHub.Core.Models;

namespace AutomationHub.Core.Interfaces;

public interface IEventProcessor
{
    void ProcessEvent(DomainEvent domainEvent);
}