using System.Text.Json.Serialization;

namespace PaymentService.DTOs;

public class ProcessPaymentDto
{
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Card";

    // Auto-populated internally from JWT token claims (hidden from Swagger request body)
    [JsonIgnore]
    public int? UserId { get; set; }

    [JsonIgnore]
    public string? UserEmail { get; set; }

    [JsonIgnore]
    public string? UserName { get; set; }
}

public class PaymentDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class RefundDto
{
    public int PaymentId { get; set; }
    public decimal RefundAmount { get; set; }
}

public class CreateOrderDto
{
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
}

public class VerifySignatureDto
{
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string RazorpayOrderId { get; set; } = "";
    public string RazorpayPaymentId { get; set; } = "";
    public string RazorpaySignature { get; set; } = "";
    public int UserId { get; set; }
    public string UserEmail { get; set; } = "";
    public string UserName { get; set; } = "";
}

public class ReportFailureDto
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public string UserEmail { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Reason { get; set; } = "Payment failed by gateway";
}
