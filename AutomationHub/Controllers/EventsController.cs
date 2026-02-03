using AutomationHub.Controllers.DTOs;
using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using AutomationHub.Core.Models.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AutomationHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventProcessor eventProcessor) : ControllerBase
{
    [HttpPost]
    public IActionResult CreateEvent([FromBody] EventCreateDto dto)
    {
        if (!Enum.TryParse<EventType>(dto.Type, out var eventType))
            return BadRequest("Invalid event type.");

        var domainEvent = DomainEvent.Create(type: eventType, source: dto.Source, payload: dto.Payload);

        eventProcessor.ProcessEvent(domainEvent);

        return Accepted(nameof(CreateEvent), new { id = domainEvent.Id });
    }
}

