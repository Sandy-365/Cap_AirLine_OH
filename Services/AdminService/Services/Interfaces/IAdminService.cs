using AdminService.DTOs;

namespace AdminService.Interfaces;

public interface IAdminService
{
    Task<DashboardDto> GetDashboardAsync();
    Task<IEnumerable<BookingReportDto>> GetBookingReportAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<RevenueReportDto>> GetRevenueReportAsync(DateTime startDate, DateTime endDate);
}
