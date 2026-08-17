using BackOfficeService.DTOs;

namespace BackOfficeService.Services.Interfaces;

public interface IBackofficeService
{
    Task<IEnumerable<BookingReportDto>> GetBookingReportAsync(DateTime startDate, DateTime endDate);
}

