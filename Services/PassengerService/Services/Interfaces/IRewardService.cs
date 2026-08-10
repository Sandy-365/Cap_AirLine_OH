using PassengerService.DTOs;

namespace PassengerService.Services.Interfaces;

public interface IRewardService
{
    Task<RewardDto> EarnPointsAsync(int userId, int points, string transactionType, int? bookingId = null);
    Task<RewardBalanceDto> GetBalanceAsync(int userId);
    Task<IEnumerable<RewardDto>> GetHistoryAsync(int userId);
    Task<RewardDto> RedeemPointsAsync(int userId, int points);
}
