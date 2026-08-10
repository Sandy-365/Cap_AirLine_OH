using Microsoft.EntityFrameworkCore;
using PassengerService.Data;
using PassengerService.Models;
using PassengerService.Repositories.Interfaces;

namespace PassengerService.Repositories.implementations;

public class PassengerProfileRepository : IPassengerProfileRepository
{
    private readonly PassengerDbContext _context;

    public PassengerProfileRepository(PassengerDbContext context)
    {
        _context = context;
    }




    /// <summary>
    /// Fetches a single passenger profile by primary key ID. Returns null if not found.
    /// </summary>
    /// 
    /// <param name="id"></param>
    /// 
    /// <returns>Task<PassengerProfile?></returns>
    /// 
    public async Task<PassengerProfile?> GetByIdAsync(int id)
        => await _context.PassengerProfiles
            .Include(p => p.SavedPassengers)
            .FirstOrDefaultAsync(p => p.Id == id);




    /// <summary>
    /// Looks up a passenger profile by email address (case-insensitive). Returns null if not found.
    /// </summary>
    /// 
    /// <param name="email"></param>
    /// 
    /// <returns></returns>
    public async Task<PassengerProfile?> GetByEmailAsync(string email)
        => await _context.PassengerProfiles
            .Include(p => p.SavedPassengers)
            .FirstOrDefaultAsync(p => p.Email.ToLower() == email.ToLower());





    /// <summary>
    /// Retrieves all passenger profiles from the database.
    /// </summary>
    /// <returns>Task<List<PassengerProfile>></returns>
    public async Task<List<PassengerProfile>> GetAllAsync()
        => await _context.PassengerProfiles
            .Include(p => p.SavedPassengers)
            .ToListAsync();





    /// <summary>
    /// Inserts a new passenger profile into the database and persists changes.
    /// </summary>
    /// 
    /// <param name="profile"></param>
    /// 
    /// <returns></returns>
    public async Task AddAsync(PassengerProfile profile)
    {
        await _context.PassengerProfiles.AddAsync(profile);
        await _context.SaveChangesAsync();
    }




    /// <summary>
    /// Updates an existing passenger profile in the database and persists changes.
    /// </summary>
    /// 
    /// <param name="profile"></param>
    /// 
    /// <returns></returns>
    public async Task UpdateAsync(PassengerProfile profile)
    {
        _context.PassengerProfiles.Update(profile);
        await _context.SaveChangesAsync();
    }
}
