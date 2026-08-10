using PassengerService.Models;

namespace PassengerService.Repositories.Interfaces;

public interface IRewardRepository
{
    Task<Reward> AddAsync(Reward reward);
    Task<IEnumerable<Reward>> GetByUserIdAsync(int userId);
    Task<int> GetTotalPointsAsync(int userId);
    Task<IEnumerable<Reward>> GetAllAsync();
}
