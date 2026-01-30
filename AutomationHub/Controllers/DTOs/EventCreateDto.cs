namespace AutomationHub.Controllers.DTOs;

public class EventCreateDto
{
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public Dictionary<string, object>? Payload { get; set; } = [];
}