using BackOfficeService.Data;
using BackOfficeService.Models;
using BackOfficeService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackOfficeService.Repositories.Implementations;

public class BackofficeProfileRepository : IBackofficeProfileRepository
{
    private readonly BackOfficeDbContext _db;

    public BackofficeProfileRepository(BackOfficeDbContext db)
    {
        _db = db;
    }

    public async Task<BackofficeProfile?> GetByIdAsync(int id)
    {
        return await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<BackofficeProfile?> GetByEmailAsync(string email)
    {
        return await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

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

    public async Task AddAsync(BackofficeProfile profile)
    {
        await _db.BackofficeProfiles.AddAsync(profile);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(BackofficeProfile profile)
    {
        _db.BackofficeProfiles.Update(profile);
        await _db.SaveChangesAsync();
    }

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
