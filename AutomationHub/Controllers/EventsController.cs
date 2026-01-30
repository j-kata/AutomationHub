using AutomationHub.Controllers.DTOs;
using AutomationHub.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutomationHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    [HttpPost]
    public IActionResult CreateEvent([FromBody] EventCreateDto dto)
    {
        if (!Enum.TryParse<EventType>(dto.Type, out var eventType))
            return BadRequest("Invalid event type.");

        var domainEvent = DomainEvent.Create(type: eventType, source: dto.Source, payload: dto.Payload);

        return CreatedAtAction(nameof(CreateEvent), new { id = domainEvent.Id }, domainEvent);
    }
}