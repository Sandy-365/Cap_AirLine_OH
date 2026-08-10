using AdminService.DTOs;
using AdminService.Interfaces;
using System.Net.Http.Json;

namespace AdminService.Services;

public class AdminServiceImpl : IAdminService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AdminServiceImpl(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }




    /// <summary>
    /// Aggregates dashboard data by making HTTP calls to BookingService, FlightService, 
    /// AdminAuth, PassengerAuth, and StaffAuth services.
    /// Counts total bookings, active flights, and users across all decentralized auth services.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<DashboardDto> GetDashboardAsync()
    {
        var totalBookings = 0;
        var totalRevenue = 0m;
        var activeFlights = 0;
        var totalUsers = 0;

        var bookingServiceUrl = _configuration["ServiceUrls:BookingService"] ?? throw new InvalidOperationException("BookingService URL is not configured");
        var flightServiceUrl = _configuration["ServiceUrls:FlightService"] ?? throw new InvalidOperationException("FlightService URL is not configured");
        var adminAuthUrl = _configuration["ServiceUrls:AdminAuth"] ?? throw new InvalidOperationException("AdminAuth URL is not configured");
        var passengerAuthUrl = _configuration["ServiceUrls:PassengerAuth"] ?? throw new InvalidOperationException("PassengerAuth URL is not configured");
        var staffAuthUrl = _configuration["ServiceUrls:StaffAuth"] ?? throw new InvalidOperationException("StaffAuth URL is not configured");

        try
        {
            var bookingResponse = await _httpClient.GetAsync($"{bookingServiceUrl}/api/bookings");
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
            Console.WriteLine($"[Warning] BookingService unreachable: {ex.Message}");
        }

        try
        {
            var flightResponse = await _httpClient.GetAsync($"{flightServiceUrl}/api/flights");
            if (flightResponse.IsSuccessStatusCode)
            {
                var flights = await flightResponse.Content.ReadFromJsonAsync<IEnumerable<object>>();
                activeFlights = flights?.Count() ?? 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] FlightService unreachable: {ex.Message}");
        }

        try
        {
            var adminAuthResponse = await _httpClient.GetAsync($"{adminAuthUrl}/api/auth/users");
            if (adminAuthResponse.IsSuccessStatusCode)
            {
                var admins = await adminAuthResponse.Content.ReadFromJsonAsync<IEnumerable<object>>();
                totalUsers += admins?.Count() ?? 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] AdminAuth unreachable: {ex.Message}");
        }

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

        try
        {
            var staffAuthResponse = await _httpClient.GetAsync($"{staffAuthUrl}/api/auth/users");
            if (staffAuthResponse.IsSuccessStatusCode)
            {
                var staff = await staffAuthResponse.Content.ReadFromJsonAsync<IEnumerable<object>>();
                totalUsers += staff?.Count() ?? 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] StaffAuth unreachable: {ex.Message}");
        }

        return new DashboardDto
        {
            TotalBookings = totalBookings,
            TotalRevenue = totalRevenue,
            ActiveFlights = activeFlights,
            TotalUsers = totalUsers
        };
    }





    /// <summary>
    /// Fetches all bookings from BookingService and filters 
    /// them client-side by the provided date range. 
    /// Returns an empty list if the service call fails.
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public async Task<IEnumerable<BookingReportDto>> GetBookingReportAsync(DateTime startDate, DateTime endDate)
    {
        var bookingServiceUrl = _configuration["ServiceUrls:BookingService"];
        var response = await _httpClient.GetAsync($"{bookingServiceUrl}/api/bookings");
        
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





    /// <summary>
    /// Retrieves bookings from BookingService, filters by date range and confirmed status,
    /// then groups by date to calculate daily revenue totals and booking counts.
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public async Task<IEnumerable<RevenueReportDto>> GetRevenueReportAsync(DateTime startDate, DateTime endDate)
    {
        var bookingServiceUrl = _configuration["ServiceUrls:BookingService"];
        var response = await _httpClient.GetAsync($"{bookingServiceUrl}/api/bookings");
        
        if (response.IsSuccessStatusCode)
        {
            // We use a temporary anonymous type to read PNR, Status, TotalAmount and CreatedAt
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
