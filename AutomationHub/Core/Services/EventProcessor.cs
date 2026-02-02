using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;

namespace AutomationHub.Core.Services;

public class EventProcessor : IEventProcessor
{
    public void ProcessEvent(DomainEvent domainEvent)
    {
        // TODO: Implement event processing logic here
        Console.WriteLine($"Processing event {domainEvent.Id} of type {domainEvent.Type} from source {domainEvent.Source}");
    }
}