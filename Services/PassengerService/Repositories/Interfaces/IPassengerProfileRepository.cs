using PassengerService.Models;

namespace PassengerService.Repositories.Interfaces;

public interface IPassengerProfileRepository
{
    Task<PassengerProfile?> GetByIdAsync(int id);
    Task<PassengerProfile?> GetByEmailAsync(string email);
    Task<List<PassengerProfile>> GetAllAsync();
    Task AddAsync(PassengerProfile profile);
    Task UpdateAsync(PassengerProfile profile);
}
