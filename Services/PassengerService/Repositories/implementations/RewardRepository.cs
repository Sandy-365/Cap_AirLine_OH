using PassengerService.Models;
using PassengerService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace PassengerService.Repositories.implementations;

public class RewardRepository : IRewardRepository
{
    private readonly PassengerService.Data.PassengerDbContext _context;

    public RewardRepository(PassengerService.Data.PassengerDbContext context)
    {
        _context = context;
    }




    /// <summary>
    /// Inserts a new reward transaction record and returns the saved entity with generated ID.
    /// </summary>
    /// 
    /// <param name="reward"></param>
    /// 
    /// <returns>Task<Reward></returns>
    public async Task<Reward> AddAsync(Reward reward)
    {
        _context.Rewards.Add(reward);
        await _context.SaveChangesAsync();
        return reward;
    }






    /// <summary>
    /// Retrieves all reward transactions for a specific user.
    /// </summary>
    /// 
    /// <param name="userId"></param>
    /// 
    /// <returns>Task<IEnumerable<Reward>></returns>
    public async Task<IEnumerable<Reward>> GetByUserIdAsync(int userId)
    {
        return await _context.Rewards.Where(r => r.UserId == userId).ToListAsync();
    }






    /// <summary>
    /// Calculates the sum of all reward points for a user, including negative redemption values
    /// </summary>
    /// 
    /// <param name="userId"></param>
    /// 
    /// <returns>Task<int></returns>
    public async Task<int> GetTotalPointsAsync(int userId)
    {
        return await _context.Rewards
            .Where(r => r.UserId == userId)
            .SumAsync(r => r.Points);
    }






    /// <summary>
    ///  Retrieves all reward transaction records from the database.
    /// </summary>
    /// <returns>Task<IEnumerable<Reward>></returns>
    public async Task<IEnumerable<Reward>> GetAllAsync()
    {
        return await _context.Rewards.ToListAsync();
    }
}
