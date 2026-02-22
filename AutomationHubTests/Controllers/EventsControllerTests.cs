using AutomationHub.Controllers;
using AutomationHub.Controllers.DTOs;
using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace AutomationHubTests.Controllers;

public class EventsControllerTests
{
    private readonly Mock<IEventProcessor> _eventProcessor;
    private readonly Mock<ILogger<EventsController>> _logger;
    private readonly EventsController _controller;

    public EventsControllerTests()
    {
        _eventProcessor = new Mock<IEventProcessor>();
        _logger = new Mock<ILogger<EventsController>>();
        _controller = new EventsController(_eventProcessor.Object, _logger.Object);
    }

    [Fact]
    public async Task CreateEvent_EventTypeValid_ShouldReturnAccepted()
    {
        // Arrange
        var dto = new EventCreateDto
        {
            Type = "TemperatureReading",
            Source = "LivingRoomSensor",
            Payload = new Dictionary<string, object> { { "temperature", 25 } }
        };

        // Act
        var result = await _controller.CreateEvent(dto);

        // Assert
        var acceptedResult = result.Should().BeOfType<AcceptedResult>().Subject;
        acceptedResult.Value.Should().NotBeNull();
        acceptedResult.Value.GetType().GetProperty("id").Should().NotBeNull();
        _eventProcessor.Verify(ep => ep.ProcessEvent(It.IsAny<DomainEvent>()), Times.Once);
    }

    [Fact]
    public async Task CreateEvent_EventTypeInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new EventCreateDto
        {
            Type = "InvalidType",
            Source = "LivingRoomSensor",
            Payload = new Dictionary<string, object> { { "temperature", 25 } }
        };

        // Act
        var result = await _controller.CreateEvent(dto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid event type.");
        _eventProcessor.Verify(ep => ep.ProcessEvent(It.IsAny<DomainEvent>()), Times.Never);
    }
}