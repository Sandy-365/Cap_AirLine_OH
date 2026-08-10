using FlightOpsService.DTOs;
using FlightOpsService.Models;
using FlightOpsService.Repositories;

namespace FlightOpsService.Services;

public interface ICheckInService
{
    Task<CheckInDto> OnlineCheckInAsync(OnlineCheckInDto dto, string passengerName, string flightNumber, int flightId, DateTime departureTime, decimal fare, string token);
    Task<CheckInDto> StaffCheckInAsync(StaffCheckInDto dto);
    Task<CheckInDto> GetCheckInAsync(int id);
    Task<BoardingPassDto> GenerateBoardingPassAsync(int checkInId);
    Task<IEnumerable<BoardingPassDto>> GetBoardingPassesByBookingAsync(int bookingId);
    Task<CheckInSummaryDto> GetSummaryAsync();
    Task<IEnumerable<CheckInDto>> GetAllCheckInsAsync();
}

public class CheckInServiceImpl : ICheckInService
{
    private readonly ICheckInRepository _repository;
    private readonly ILogger<CheckInServiceImpl> _logger;

    public CheckInServiceImpl(
        ICheckInRepository repository,
        ILogger<CheckInServiceImpl> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CheckInDto> OnlineCheckInAsync(OnlineCheckInDto dto, string passengerName, string flightNumber, int flightId, DateTime departureTime, decimal fare, string token)
    {
        // 0. Check if it's too early for check-in (Must be within 5 hours)
        var nowIst = DateTime.UtcNow.AddHours(5.5);
        var timeUntilDeparture = departureTime - nowIst;
        
        if (timeUntilDeparture.TotalHours > 5)
        {
            throw new InvalidOperationException($"Check-in for flight {flightNumber} opens only 5 hours before departure. Current IST: {nowIst:HH:mm}, Departure IST: {departureTime:HH:mm}");
        }
        
        if (timeUntilDeparture.TotalHours < 0)
        {
            throw new InvalidOperationException($"Flight {flightNumber} has already departed.");
        }

        // 1. Check if already checked in
        var existing = await _repository.GetByPassengerIdAsync(dto.PassengerId);
        if (existing != null) return MapToDto(existing);

        var seatNumber = !string.IsNullOrWhiteSpace(dto.SeatNumber) ? dto.SeatNumber : GenerateSeatNumber();
        var qrCode = GenerateQRCode($"{flightNumber}-{seatNumber}");

        var checkIn = new CheckIn
        {
            BookingId = dto.BookingId,
            PassengerId = dto.PassengerId,
            UserId = dto.UserId,
            FlightId = flightId,
            SeatNumber = seatNumber,
            Gate = "TBD",
            BoardingPass = $"{passengerName}|{flightNumber}|{seatNumber}",
            QRCode = qrCode,
            CheckInTime = DateTime.UtcNow,
            IsCheckedIn = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(checkIn);

        _logger.LogInformation("Online Check-in completed for Passenger {PassengerId}", dto.PassengerId);
        return MapToDto(checkIn);
    }

    public async Task<CheckInDto> StaffCheckInAsync(StaffCheckInDto dto)
    {
        var existing = await _repository.GetByPassengerIdAsync(dto.PassengerId);
        if (existing != null) return MapToDto(existing);

        var seatNumber = !string.IsNullOrWhiteSpace(dto.SeatNumber) ? dto.SeatNumber : GenerateSeatNumber();
        var qrCode = GenerateQRCode($"{dto.FlightNumber}-{seatNumber}");

        var checkIn = new CheckIn
        {
            BookingId = dto.BookingId,
            PassengerId = dto.PassengerId,
            UserId = dto.UserId, 
            FlightId = dto.FlightId,
            SeatNumber = seatNumber,
            Gate = dto.Gate ?? "G1",
            BoardingPass = $"{dto.PassengerName}|{dto.FlightNumber}|{seatNumber}",
            QRCode = qrCode,
            CheckInTime = DateTime.UtcNow,
            IsCheckedIn = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(checkIn);

        _logger.LogInformation("Staff Check-in completed for Passenger {PassengerId}", dto.PassengerId);
        return MapToDto(checkIn);
    }

    public async Task<CheckInDto> GetCheckInAsync(int id)
    {
        var checkIn = await _repository.GetByIdAsync(id);
        if (checkIn == null) throw new KeyNotFoundException($"Check-in {id} not found");
        return MapToDto(checkIn);
    }

    public async Task<BoardingPassDto> GenerateBoardingPassAsync(int checkInId)
    {
        var checkIn = await _repository.GetByIdAsync(checkInId);
        if (checkIn == null) throw new KeyNotFoundException($"Check-in {checkInId} not found");

        var parts = checkIn.BoardingPass.Split('|');
        return new BoardingPassDto
        {
            PassengerName = parts[0],
            FlightNumber = parts[1],
            SeatNumber = checkIn.SeatNumber,
            Gate = checkIn.Gate,
            QRCode = checkIn.QRCode
        };
    }

    public async Task<IEnumerable<BoardingPassDto>> GetBoardingPassesByBookingAsync(int bookingId)
    {
        var checkIns = await _repository.GetByBookingIdAsync(bookingId);
        return checkIns.Select(c => {
            var parts = c.BoardingPass.Split('|');
            return new BoardingPassDto
            {
                PassengerName = parts[0],
                FlightNumber = parts[1],
                SeatNumber = c.SeatNumber,
                Gate = c.Gate,
                QRCode = c.QRCode
            };
        });
    }

    public async Task<CheckInSummaryDto> GetSummaryAsync()
    {
        var all = await _repository.GetAllAsync();
        var today = DateTime.UtcNow.Date;
        return new CheckInSummaryDto
        {
            TotalCheckIns = all.Count(),
            TodayCheckIns = all.Count(c => c.CheckInTime.Date == today)
        };
    }

    public async Task<IEnumerable<CheckInDto>> GetAllCheckInsAsync()
    {
        var all = await _repository.GetAllAsync();
        return all.Select(MapToDto).OrderByDescending(c => c.CheckInTime);
    }

    private string GenerateSeatNumber() => $"{Random.Shared.Next(1, 51)}{(char)('A' + Random.Shared.Next(0, 6))}";

    private string GenerateQRCode(string data)
    {
        using var qrGenerator = new QRCoder.QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(data, QRCoder.QRCodeGenerator.ECCLevel.Q);
        return Convert.ToBase64String(new QRCoder.PngByteQRCode(qrCodeData).GetGraphic(10));
    }

    private CheckInDto MapToDto(CheckIn c)
    {
        var parts = c.BoardingPass.Split('|');
        return new CheckInDto
        {
            Id = c.Id,
            BookingId = c.BookingId,
            PassengerId = c.PassengerId,
            PassengerName = parts.Length > 0 ? parts[0] : "Unknown",
            FlightNumber = parts.Length > 1 ? parts[1] : "TBD",
            SeatNumber = c.SeatNumber,
            Gate = c.Gate,
            BoardingPass = c.BoardingPass,
            CheckInTime = c.CheckInTime
        };
    }
}
