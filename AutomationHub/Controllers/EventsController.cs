using AutomationHub.Controllers.DTOs;
using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AutomationHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventProcessor eventProcessor, ILogger<EventsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] EventCreateDto dto)
    {
        logger.LogInformation("Received event: Type{EventType}, Source{EventSource}", dto.Type, dto.Source);

        if (!Enum.TryParse<EventType>(dto.Type, out var eventType))
        {
            logger.LogWarning("Invalid event type: {EventType}", dto.Type);
            return BadRequest("Invalid event type.");
        }

        var domainEvent = DomainEvent.Create(type: eventType, source: dto.Source, payload: dto.Payload);
        logger.LogInformation("Accepted event: {EventId}", domainEvent.Id);

        await eventProcessor.ProcessEvent(domainEvent);

        return Accepted(nameof(CreateEvent), new { id = domainEvent.Id });
    }
}

