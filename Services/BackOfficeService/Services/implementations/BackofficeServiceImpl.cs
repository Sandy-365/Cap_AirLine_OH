using BackOfficeService.DTOs;
using BackOfficeService.Services.Interfaces;
using System.Net.Http.Json;

namespace BackOfficeService.Services.Implementations;

public class BackofficeServiceImpl : IBackofficeService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public BackofficeServiceImpl(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<IEnumerable<BookingReportDto>> GetBookingReportAsync(DateTime startDate, DateTime endDate)
    {
        var flightOpsUrl = _configuration["ServiceUrls:FlightOpsService"] ?? "http://localhost:5002";
        var response = await _httpClient.GetAsync($"{flightOpsUrl}/api/bookings");
        
        if (response.IsSuccessStatusCode)
        {
            var bookings = await response.Content.ReadFromJsonAsync<IEnumerable<BookingReportDto>>();
            if (bookings != null)
            {
                return bookings.Where(b => b.CreatedAt >= startDate && b.CreatedAt <= endDate);
            }
        }
        
        return new List<BookingReportDto>();
    }
}
