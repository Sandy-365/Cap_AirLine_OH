namespace PassengerService.DTOs;

public class RewardDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Points { get; set; }
    public string TransactionType { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class RewardBalanceDto
{
    public int UserId { get; set; }
    public int TotalPoints { get; set; }
}

public class RedeemRewardDto
{
    public int UserId { get; set; }
    public int Points { get; set; }
}

public class EarnPointsRequest
{
    public int UserId { get; set; }
    public int Points { get; set; }
    public string TransactionType { get; set; } = "";
    public int? BookingId { get; set; }
}

public class RedeemPointsRequest
{
    public int UserId { get; set; }
    public int Points { get; set; }
}

