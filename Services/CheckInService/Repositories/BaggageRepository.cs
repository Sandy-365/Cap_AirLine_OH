using CheckInService.Data;
using CheckInService.Models;
using Microsoft.EntityFrameworkCore;

namespace CheckInService.Repositories;

public interface IBaggageRepository
{
    Task<Baggage?> GetByIdAsync(int id);
    Task<Baggage?> GetByTrackingNumberAsync(string trackingNumber);
    Task<Baggage> AddAsync(Baggage baggage);
    Task UpdateAsync(Baggage baggage);
    Task<IEnumerable<Baggage>> GetByBookingIdAsync(int bookingId);
    Task<IEnumerable<Baggage>> GetAllAsync();
}

public class BaggageRepository : IBaggageRepository
{
    private readonly CheckInDbContext _context;

    public BaggageRepository(CheckInDbContext context)
    {
        _context = context;
    }

    public async Task<Baggage?> GetByIdAsync(int id)
    {
        return await _context.Baggages.FindAsync(id);
    }

    public async Task<Baggage?> GetByTrackingNumberAsync(string trackingNumber)
    {
        return await _context.Baggages.FirstOrDefaultAsync(b => b.TrackingNumber == trackingNumber);
    }

    public async Task<Baggage> AddAsync(Baggage baggage)
    {
        _context.Baggages.Add(baggage);
        await _context.SaveChangesAsync();
        return baggage;
    }

    public async Task UpdateAsync(Baggage baggage)
    {
        _context.Baggages.Update(baggage);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Baggage>> GetByBookingIdAsync(int bookingId)
    {
        return await _context.Baggages.Where(b => b.BookingId == bookingId).ToListAsync();
    }

    public async Task<IEnumerable<Baggage>> GetAllAsync()
    {
        return await _context.Baggages.ToListAsync();
    }
}
