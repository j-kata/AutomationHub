using AutomationHub.Core.Models;

namespace AutomationHub.Core.Interfaces;

public interface IMqttParser
{
    bool TryParse(string topic, string payload, out DomainEvent? domainEvent);
    DomainEvent Parse(string topic, string payload);
}