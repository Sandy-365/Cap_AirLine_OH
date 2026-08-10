using System.Net;
using System.Net.Http.Json;
using AdminService.DTOs;
using AdminService.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;

namespace AdminService.Tests
{
    public class AdminServiceTests
    {
        private Mock<HttpMessageHandler> _handlerMock;
        private HttpClient _httpClient;
        private Mock<IConfiguration> _configMock;
        private AdminServiceImpl _adminService;

        public AdminServiceTests()
        {
            _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _httpClient = new HttpClient(_handlerMock.Object);
            _configMock = new Mock<IConfiguration>();
            
            _configMock.Setup(x => x["ServiceUrls:BookingService"]).Returns("http://booking-service");
            _configMock.Setup(x => x["ServiceUrls:FlightService"]).Returns("http://flight-service");
            _configMock.Setup(x => x["ServiceUrls:AdminAuth"]).Returns("http://admin-auth");
            _configMock.Setup(x => x["ServiceUrls:PassengerAuth"]).Returns("http://passenger-auth");
            _configMock.Setup(x => x["ServiceUrls:StaffAuth"]).Returns("http://staff-auth");

            _adminService = new AdminServiceImpl(_httpClient, _configMock.Object);
        }

        [Fact]
        public async Task GetDashboardAsync_ShouldReturnAggregatedData()
        {
            // Arrange
            var bookings = new List<object> { new { Id = 1 }, new { Id = 2 } };
            var flights = new List<object> { new { Id = 101 } };
            var admins = new List<object> { new { Id = 1 } };
            var passengers = new List<object> { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } };
            var staff = new List<object> { new { Id = 1 } };

            SetupMockResponse("http://booking-service/api/bookings", bookings);
            SetupMockResponse("http://flight-service/api/flights", flights);
            SetupMockResponse("http://admin-auth/api/auth/users", admins);
            SetupMockResponse("http://passenger-auth/api/auth/users", passengers);
            SetupMockResponse("http://staff-auth/api/auth/users", staff);

            // Act
            var result = await _adminService.GetDashboardAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalBookings);
            Assert.Equal(1, result.ActiveFlights);
            Assert.Equal(5, result.TotalUsers); // 1 admin + 3 passengers + 1 staff
        }

        [Fact]
        public async Task GetRevenueReportAsync_ShouldFilterAndGroupConfirmedBookings()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);
            
            var rawData = new List<object>
            {
                new { Status = "Confirmed", TotalAmount = 1000m, CreatedAt = today },
                new { Status = "Confirmed", TotalAmount = 500m, CreatedAt = today },
                new { Status = "Confirmed", TotalAmount = 2000m, CreatedAt = yesterday },
                new { Status = "Cancelled", TotalAmount = 5000m, CreatedAt = today } // Should be ignored
            };

            SetupMockResponse("http://booking-service/api/bookings", rawData);

            // Act
            var result = (await _adminService.GetRevenueReportAsync(yesterday, today)).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            
            var yesterdayReport = result.First(r => r.Date == yesterday);
            Assert.Equal(2000m, yesterdayReport.Revenue);
            Assert.Equal(1, yesterdayReport.BookingCount);

            var todayReport = result.First(r => r.Date == today);
            Assert.Equal(1500m, todayReport.Revenue);
            Assert.Equal(2, todayReport.BookingCount);
        }

        private void SetupMockResponse(string url, object responseData)
        {
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(responseData)
                });
        }
    }
}
