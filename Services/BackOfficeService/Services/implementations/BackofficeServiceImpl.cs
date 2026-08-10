using BackOfficeService.Data;
using BackOfficeService.DTOs;
using BackOfficeService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace BackOfficeService.Services.Implementations;

public class BackofficeServiceImpl : IBackofficeService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly BackOfficeDbContext _db;

    public BackofficeServiceImpl(
        HttpClient httpClient,
        IConfiguration configuration,
        BackOfficeDbContext db)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _db = db;
    }

    /// <summary>
    /// Aggregates dashboard metrics by querying FlightOpsService and PassengerService over HTTP,
    /// and counting backoffice users directly from BackofficeProfiles table.
    /// </summary>
    public async Task<DashboardDto> GetDashboardAsync()
    {
        var totalBookings = 0;
        var totalRevenue = 0m;
        var activeFlights = 0;
        var totalUsers = 0;

        var flightOpsUrl = _configuration["ServiceUrls:FlightOpsService"] ?? "http://localhost:5002";
        var passengerAuthUrl = _configuration["ServiceUrls:PassengerAuth"] ?? "http://localhost:5007";

        // 1. Fetch Bookings stats from FlightOpsService
        try
        {
            var bookingResponse = await _httpClient.GetAsync($"{flightOpsUrl}/api/bookings");
            if (bookingResponse.IsSuccessStatusCode)
            {
                var bookings = await bookingResponse.Content.ReadFromJsonAsync<IEnumerable<RawBookingData>>();
                if (bookings != null)
                {
                    totalBookings = bookings.Count();
                    totalRevenue = bookings.Where(b => b.Status == "Confirmed").Sum(b => b.TotalAmount);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] FlightOpsService (bookings) unreachable: {ex.Message}");
        }

        // 2. Fetch Flights stats from FlightOpsService
        try
        {
            var flightResponse = await _httpClient.GetAsync($"{flightOpsUrl}/api/flights");
            if (flightResponse.IsSuccessStatusCode)
            {
                var flights = await flightResponse.Content.ReadFromJsonAsync<IEnumerable<object>>();
                activeFlights = flights?.Count() ?? 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] FlightOpsService (flights) unreachable: {ex.Message}");
        }

        // 3. Count Backoffice Users directly from Database
        try
        {
            totalUsers += await _db.BackofficeProfiles.CountAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] BackofficeProfiles count failed: {ex.Message}");
        }

        // 4. Count Passengers via HTTP to PassengerService (port 5007)
        try
        {
            var passengerAuthResponse = await _httpClient.GetAsync($"{passengerAuthUrl}/api/auth/users");
            if (passengerAuthResponse.IsSuccessStatusCode)
            {
                var passengers = await passengerAuthResponse.Content.ReadFromJsonAsync<IEnumerable<object>>();
                totalUsers += passengers?.Count() ?? 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] PassengerAuth unreachable: {ex.Message}");
        }

        return new DashboardDto
        {
            TotalBookings = totalBookings,
            TotalRevenue = totalRevenue,
            ActiveFlights = activeFlights,
            TotalUsers = totalUsers
        };
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

    public async Task<IEnumerable<RevenueReportDto>> GetRevenueReportAsync(DateTime startDate, DateTime endDate)
    {
        var flightOpsUrl = _configuration["ServiceUrls:FlightOpsService"] ?? "http://localhost:5002";
        var response = await _httpClient.GetAsync($"{flightOpsUrl}/api/bookings");
        
        if (response.IsSuccessStatusCode)
        {
            var bookings = await response.Content.ReadFromJsonAsync<IEnumerable<RawBookingData>>();
            if (bookings != null)
            {
                return bookings
                    .Where(b => b.CreatedAt >= startDate && b.CreatedAt <= endDate && b.Status == "Confirmed")
                    .GroupBy(b => b.CreatedAt.Date)
                    .Select(g => new RevenueReportDto
                    {
                        Date = g.Key,
                        Revenue = g.Sum(b => b.TotalAmount),
                        BookingCount = g.Count()
                    })
                    .OrderBy(r => r.Date);
            }
        }
        
        return new List<RevenueReportDto>();
    }

    private class RawBookingData
    {
        public string Status { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
