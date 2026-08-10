
using FlightService.DTOs;
using FlightService.Models;
using FlightService.Repositories;
using FlightService.Services;
using Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlightService.Tests
{
    public class FlightServiceTests
    {
        private readonly Mock<IFlightRepository> _repoMock;
        private readonly Mock<ILogger<FlightService.Services.FlightService>> _loggerMock;
        private readonly FlightService.Services.FlightService _flightService;

        public FlightServiceTests()
        {
            _repoMock = new Mock<IFlightRepository>();
            _loggerMock = new Mock<ILogger<FlightService.Services.FlightService>>();
            
            _flightService = new FlightService.Services.FlightService(
                _repoMock.Object, 
                _loggerMock.Object);
        }


        [Fact]
        public async Task DelayFlightAsync_ShouldUpdateStatusAndPublishEvent()
        {
            // Arrange
            var flightId = 1;
            var newTime = DateTime.UtcNow.AddHours(5);
            var flight = new Flight { Id = flightId, FlightNumber = "SK123", Status = FlightStatus.Scheduled };

            _repoMock.Setup(r => r.GetByIdAsync(flightId)).ReturnsAsync(flight);

            // Act
            await _flightService.DelayFlightAsync(flightId, newTime);

            // Assert
            Assert.Equal(FlightStatus.Delayed, flight.Status);
            Assert.Equal(newTime, flight.DepartureTime);
            _repoMock.Verify(r => r.UpdateAsync(flight), Times.Once);
        }
    }
}
