using BackOfficeService.Controllers;
using BackOfficeService.DTOs;
using BackOfficeService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BackOfficeService.Tests;

public class BackofficeControllerTests
{
    private readonly Mock<IBackofficeService> _backofficeServiceMock;
    private readonly Mock<IBackofficeAuthService> _authServiceMock;
    private readonly BackofficeController _controller;

    public BackofficeControllerTests()
    {
        _backofficeServiceMock = new Mock<IBackofficeService>();
        _authServiceMock = new Mock<IBackofficeAuthService>();
        _controller = new BackofficeController(_backofficeServiceMock.Object, _authServiceMock.Object);
    }

    [Fact]
    public async Task GetBookingReport_ReturnsOkWithReportData()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);
        var endDate = new DateTime(2026, 1, 31);
        var expectedReports = new List<BookingReportDto>
        {
            new() {
                BookingId = 101,
                UserId = 5,
                FlightId = 12,
                Status = "Confirmed",
                CreatedAt = new DateTime(2026, 1, 15)
            }
        };

        _backofficeServiceMock
            .Setup(s => s.GetBookingReportAsync(startDate, endDate))
            .ReturnsAsync(expectedReports);

        // Act
        var result = await _controller.GetBookingReport(startDate, endDate);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var actualReports = Assert.IsAssignableFrom<IEnumerable<BookingReportDto>>(okResult.Value);
        Assert.Single(actualReports);
    }
}
