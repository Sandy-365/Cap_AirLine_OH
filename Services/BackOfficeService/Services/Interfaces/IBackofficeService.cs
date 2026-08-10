using BackOfficeService.DTOs;

namespace BackOfficeService.Services.Interfaces;

public interface IBackofficeService
{
    Task<DashboardDto> GetDashboardAsync();
    Task<IEnumerable<BookingReportDto>> GetBookingReportAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<RevenueReportDto>> GetRevenueReportAsync(DateTime startDate, DateTime endDate);
}
