using AutomationHub.Core.Models.Constants;

namespace AutomationHub.Core.Models;

public class DomainEvent
{
    public Guid Id { get; private set; }
    public EventType Type { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public Dictionary<string, object> Payload { get; private set; } = [];
    public DateTime CreatedAt { get; private set; }

    private DomainEvent() { }

    public static DomainEvent Create(EventType type, string source, Dictionary<string, object>? payload)
    {
        return new DomainEvent
        {
            Id = Guid.NewGuid(),
            Type = type,
            Source = source,
            Payload = payload ?? [],
            CreatedAt = DateTime.UtcNow
        };
    }
}