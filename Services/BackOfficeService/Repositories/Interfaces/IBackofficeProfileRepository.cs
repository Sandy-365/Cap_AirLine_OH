using BackOfficeService.Models;

namespace BackOfficeService.Repositories.Interfaces;

public interface IBackofficeProfileRepository
{
    Task<BackofficeProfile?> GetByIdAsync(int id);
    Task<BackofficeProfile?> GetByEmailAsync(string email);
    Task<List<BackofficeProfile>> GetAllAsync(string[]? roles = null);
    Task AddAsync(BackofficeProfile profile);
    Task UpdateAsync(BackofficeProfile profile);
    Task DeleteAsync(int id);
}
