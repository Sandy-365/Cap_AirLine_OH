using BackOfficeService.Data;
using BackOfficeService.Models;
using BackOfficeService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackOfficeService.Repositories.Implementations;

/// <summary>
/// Repository implementation for performing CRUD and query operations on back-office user profiles in the database.
/// </summary>
public class BackofficeProfileRepository : IBackofficeProfileRepository
{
    private readonly BackOfficeDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackofficeProfileRepository"/> class.
    /// </summary>
    /// <param name="db">The database context for the BackOffice service.</param>
    public BackofficeProfileRepository(BackOfficeDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retrieves a back-office user profile by its unique identifier.
    /// </summary>
    /// <param name="id">The unique profile ID.</param>
    /// <returns>A task representing the asynchronous operation that yields the profile if found; otherwise, null.</returns>
    public async Task<BackofficeProfile?> GetByIdAsync(int id)
    {
        return await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <summary>
    /// Retrieves a back-office user profile by its email address (case-insensitive).
    /// </summary>
    /// <param name="email">The email address of the user profile.</param>
    /// <returns>A task representing the asynchronous operation that yields the profile if found; otherwise, null.</returns>
    public async Task<BackofficeProfile?> GetByEmailAsync(string email)
    {
        return await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    /// <summary>
    /// Retrieves all back-office user profiles from the database, with optional filtering by role.
    /// </summary>
    /// <param name="roles">An optional array of role names to filter the results by.</param>
    /// <returns>A task representing the asynchronous operation that yields a list of <see cref="BackofficeProfile"/> records.</returns>
    public async Task<List<BackofficeProfile>> GetAllAsync(string[]? roles = null)
    {
        var query = _db.BackofficeProfiles.AsQueryable();
        if (roles != null && roles.Length > 0)
        {
            var roleList = roles.ToList();
            query = query.Where(u => roleList.Contains(u.Role));
        }
        return await query.ToListAsync();
    }

    /// <summary>
    /// Adds a new back-office user profile entity to the database.
    /// </summary>
    /// <param name="profile">The profile entity to add.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    public async Task AddAsync(BackofficeProfile profile)
    {
        await _db.BackofficeProfiles.AddAsync(profile);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Updates an existing back-office user profile entity in the database.
    /// </summary>
    /// <param name="profile">The modified profile entity to update.</param>
    /// <returns>A task representing the asynchronous update operation.</returns>
    public async Task UpdateAsync(BackofficeProfile profile)
    {
        _db.BackofficeProfiles.Update(profile);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a back-office user profile from the database by its identifier if it exists.
    /// </summary>
    /// <param name="id">The unique identifier of the profile to delete.</param>
    /// <returns>A task representing the asynchronous deletion operation.</returns>
    public async Task DeleteAsync(int id)
    {
        var profile = await GetByIdAsync(id);
        if (profile != null)
        {
            _db.BackofficeProfiles.Remove(profile);
            await _db.SaveChangesAsync();
        }
    }
}
