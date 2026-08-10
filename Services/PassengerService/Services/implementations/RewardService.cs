using PassengerService.DTOs;
using PassengerService.Models;
using PassengerService.Repositories.Interfaces;
using PassengerService.Services.Interfaces;

namespace PassengerService.Services.implementations;

public class RewardServiceImpl : IRewardService
{
    private readonly IRewardRepository _repository;
    private readonly ILogger<RewardServiceImpl> _logger;
    private const int PointsPerDollar = 10;

    public RewardServiceImpl(IRewardRepository repository, ILogger<RewardServiceImpl> logger)
    {
        _repository = repository;
        _logger = logger;
    }




    /// <summary>
    /// Creates a reward transaction adding points to a user's balance. Publishes a 
    /// RewardEarnedEvent when associated with a booking for downstream notification handling.
    /// </summary>
    /// 
    /// <param name="userId"></param>
    /// <param name="points"></param>
    /// <param name="transactionType"></param>
    /// <param name="bookingId"></param>
    /// 
    /// <returns>Task<RewardDto></returns>
    /// 
    public async Task<RewardDto> EarnPointsAsync(int userId, int points, string transactionType, int? bookingId = null)
    {
        var reward = new Reward
        {
            UserId = userId,
            Points = points,
            TransactionType = transactionType,
            BookingId = bookingId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(reward);

        if (bookingId.HasValue)
        {
        }

        return MapToDto(reward);
    }






    /// <summary>
    /// Calculates total reward points for a user by summing all
    /// point transactions (positive and negative).
    /// </summary>
    /// 
    /// <param name="userId"></param>
    /// 
    /// <returns>Task<RewardBalanceDto></returns>
    /// 
    public async Task<RewardBalanceDto> GetBalanceAsync(int userId)
    {
        var totalPoints = await _repository.GetTotalPointsAsync(userId);
        return new RewardBalanceDto
        {
            UserId = userId,
            TotalPoints = totalPoints
        };
    }





    /// <summary>
    /// Returns all reward transactions for a user, showing 
    /// points earned and redeemed with transaction types.
    /// </summary>
    /// 
    /// <param name="userId"></param>
    /// 
    /// <returns>Task of IEnumerable of RewardDto</returns>
    /// 
    public async Task<IEnumerable<RewardDto>> GetHistoryAsync(int userId)
    {
        var rewards = await _repository.GetByUserIdAsync(userId);
        return rewards.Select(MapToDto);
    }






    /// <summary>
    ///  Deducts points from user balance after validating sufficient points. Creates a negative-point redemption transaction.
    /// </summary>
    /// 
    /// <param name="userId"></param>
    /// <param name="points"></param>
    /// 
    /// <returns>Task of RewardDto</returns>
    /// 
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<RewardDto> RedeemPointsAsync(int userId, int points)
    {
        var balance = await GetBalanceAsync(userId);
        if (balance.TotalPoints < points)
            throw new InvalidOperationException("Insufficient points");

        var reward = new Reward
        {
            UserId = userId,
            Points = -points,
            TransactionType = "Redemption",
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(reward);
        return MapToDto(reward);
    }








    private RewardDto MapToDto(Reward reward)
    {
        return new RewardDto
        {
            Id = reward.Id,
            UserId = reward.UserId,
            Points = reward.Points,
            TransactionType = reward.TransactionType,
            CreatedAt = reward.CreatedAt
        };
    }
}
