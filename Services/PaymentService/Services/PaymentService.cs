using PaymentService.DTOs;
using PaymentService.Models;
using PaymentService.Repositories;
using Shared.Models;
using Razorpay.Api;
using System.Collections.Generic;

namespace PaymentService.Services;

public interface IPaymentService
{
    Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentDto dto);
    Task<object> CreateOrderAsync(CreateOrderDto dto);
    Task<PaymentDto> VerifySignatureAsync(VerifySignatureDto dto);
    Task<PaymentDto> GetPaymentAsync(int id);
    Task<PaymentDto> RefundAsync(int paymentId);
    Task ReportFailureAsync(ReportFailureDto dto);
}

public class PaymentServiceImpl : IPaymentService
{
    private readonly IPaymentRepository _repository;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PaymentServiceImpl> _logger;

    public PaymentServiceImpl(
        IPaymentRepository repository,
        HttpClient httpClient,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PaymentServiceImpl> logger)
    {
        _repository = repository;
        _httpClient = httpClient;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private async Task<decimal> ValidateBookingAsync(int bookingId)
    {
        try
        {
            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            var bookingServiceUrl = _configuration["ServiceUrls:FlightOpsService"] 
                ?? _configuration["ServiceUrls:BookingService"] 
                ?? "http://localhost:5002";

            var requestMessage = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"{bookingServiceUrl}/api/bookings/{bookingId}");
            if (!string.IsNullOrEmpty(token))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            _logger.LogInformation("Validating booking {BookingId} against {Url}", bookingId, bookingServiceUrl);
            var bookingResponse = await _httpClient.SendAsync(requestMessage);
            if (!bookingResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Booking validation failed: {StatusCode}", bookingResponse.StatusCode);
                throw new InvalidOperationException($"Booking {bookingId} does not exist or is not accessible");
            }

            var content = await bookingResponse.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("totalAmount", out var totalAmountProp))
            {
                return totalAmountProp.GetDecimal();
            }
            if (doc.RootElement.TryGetProperty("TotalAmount", out var totalAmountPropCap))
            {
                return totalAmountPropCap.GetDecimal();
            }
            return 0;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Unable to validate booking {BookingId}", bookingId);
            throw new InvalidOperationException($"Unable to validate booking: {ex.Message}");
        }
    }

    private async Task NotifyBookingPaymentSuccessAsync(int bookingId, string transactionId, string paymentMethod)
    {
        try
        {
            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var bookingServiceUrl = _configuration["ServiceUrls:FlightOpsService"] 
                ?? _configuration["ServiceUrls:BookingService"] 
                ?? "http://localhost:5002";

            var url = $"{bookingServiceUrl}/api/bookings/{bookingId}/confirm-payment?transactionId={Uri.EscapeDataString(transactionId)}&paymentMethod={Uri.EscapeDataString(paymentMethod)}";
            var requestMessage = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, url);
            if (!string.IsNullOrEmpty(token))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            _logger.LogInformation("Calling FlightOpsService to confirm payment for Booking {BookingId} at {Url}", bookingId, url);
            var response = await _httpClient.SendAsync(requestMessage);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully updated booking {BookingId} status to Confirmed & Paid", bookingId);
            }
            else
            {
                _logger.LogWarning("Failed to update booking {BookingId} status. Status code: {StatusCode}", bookingId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while notifying booking payment confirmation for Booking {BookingId}", bookingId);
        }
    }

    public async Task<object> CreateOrderAsync(CreateOrderDto dto)
    {
        _logger.LogInformation("CreateOrderAsync called for BookingId={BookingId}, Amount={Amount}", dto.BookingId, dto.Amount);

        // 1. Validate Booking
        var bookingTotal = await ValidateBookingAsync(dto.BookingId);
        decimal finalAmount = (dto.Amount > 0) ? dto.Amount : bookingTotal;

        // 2. Initialize RazorPay Client
        string? key = _configuration["Razorpay:KeyId"];
        string? secret = _configuration["Razorpay:KeySecret"];

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
        {
            _logger.LogError("Razorpay KeyId or KeySecret is not configured");
            throw new InvalidOperationException("Razorpay payment gateway is not configured. Contact admin.");
        }

        // 3. Validate amount
        if (finalAmount <= 0)
        {
            _logger.LogError("Invalid amount {Amount} for BookingId={BookingId}", finalAmount, dto.BookingId);
            throw new InvalidOperationException($"Invalid payment amount: {finalAmount}. Amount must be greater than 0.");
        }

        // 4. Define RazorPay Order Options
        int amountInPaise = (int)(finalAmount * 100);
        _logger.LogInformation("Creating Razorpay order: BookingId={BookingId}, Amount={Amount}, AmountInPaise={Paise}", dto.BookingId, finalAmount, amountInPaise);
        Dictionary<string, object> options = new Dictionary<string, object>();
        options.Add("amount", amountInPaise); // amount in paise MUST be int
        options.Add("currency", "INR");
        options.Add("receipt", $"booking_rcptid_{dto.BookingId}");

        try
        {
            var client = new RazorpayClient(key, secret);
            Order order = client.Order.Create(options);
            string orderId = order["id"]?.ToString() ?? throw new InvalidOperationException("Razorpay returned null order ID");
            _logger.LogInformation("Razorpay Order created: {OrderId}", orderId);

            // 4. Return full order data to Angular (key, amount, currency, orderId)
            return new
            {
                orderId = orderId,
                key = key,
                amount = amountInPaise,
                currency = "INR"
            };
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Razorpay order creation failed for BookingId={BookingId}", dto.BookingId);
            throw new InvalidOperationException($"Razorpay order creation failed: {ex.Message}");
        }
    }

    public async Task<PaymentDto> VerifySignatureAsync(VerifySignatureDto dto)
    {
        _logger.LogInformation("VerifySignatureAsync called for BookingId={BookingId}", dto.BookingId);

        string key = _configuration["Razorpay:KeyId"]!;
        string secret = _configuration["Razorpay:KeySecret"]!;

        try
        {
            var payload = dto.RazorpayOrderId + "|" + dto.RazorpayPaymentId;
            var secretBytes = System.Text.Encoding.UTF8.GetBytes(secret);
            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            using (var hmac = new System.Security.Cryptography.HMACSHA256(secretBytes))
            {
                var hashBytes = hmac.ComputeHash(payloadBytes);
                var generatedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                if (generatedSignature != dto.RazorpaySignature)
                {
                    throw new InvalidOperationException("Signature mismatch");
                }
            }
            _logger.LogInformation("Razorpay signature verified successfully for BookingId={BookingId}", dto.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Razorpay signature verification failed for BookingId={BookingId}", dto.BookingId);

            // Publish PaymentFailedEvent so Saga can cancel the booking

            throw new InvalidOperationException("Invalid RazorPay Signature. Payment Failed.");
        }

        // Signature valid, register payment with actual amount from DTO
        var payment = new PaymentService.Models.Payment
        {
            BookingId = dto.BookingId,
            Amount = dto.Amount,
            PaymentMethod = "RazorPay",
            TransactionId = dto.RazorpayPaymentId,
            Status = PaymentStatus.Success,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(payment);
        _logger.LogInformation("Payment record saved: PaymentId={PaymentId}, Amount={Amount}", payment.Id, dto.Amount);

        // Notify FlightOpsService to confirm booking and update PaymentStatus to Success/Paid
        await NotifyBookingPaymentSuccessAsync(dto.BookingId, dto.RazorpayPaymentId, "RazorPay");

        return MapToDto(payment);
    }

    public async Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentDto dto)
    {
        // Validate booking exists and get its authoritative total amount
        var bookingTotal = await ValidateBookingAsync(dto.BookingId);

        decimal finalAmount = (dto.Amount.HasValue && dto.Amount.Value > 0) ? dto.Amount.Value : bookingTotal;
        if (finalAmount <= 0)
        {
            finalAmount = bookingTotal > 0 ? bookingTotal : 0;
        }

        var transactionId = Guid.NewGuid().ToString();

        var payment = new PaymentService.Models.Payment
        {
            BookingId = dto.BookingId,
            Amount = finalAmount,
            PaymentMethod = dto.PaymentMethod,
            TransactionId = transactionId,
            Status = PaymentStatus.Success,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(payment);
        _logger.LogInformation("Payment record saved: PaymentId={PaymentId}, Amount={Amount}", payment.Id, finalAmount);

        // Notify FlightOpsService to confirm booking and update PaymentStatus to Success/Paid
        await NotifyBookingPaymentSuccessAsync(dto.BookingId, transactionId, dto.PaymentMethod);

        return MapToDto(payment);
    }

    public async Task<PaymentDto> GetPaymentAsync(int id)
    {
        var payment = await _repository.GetByIdAsync(id);
        if (payment == null)
            throw new KeyNotFoundException($"Payment {id} not found");

        return MapToDto(payment);
    }

    public async Task ReportFailureAsync(ReportFailureDto dto)
    {
        _logger.LogWarning("[PaymentService] Payment failure reported by frontend for BookingId={BookingId}, Reason={Reason}", dto.BookingId, dto.Reason);

        _logger.LogInformation("[PaymentService] PaymentFailedEvent published for BookingId={BookingId}", dto.BookingId);
    }

    public async Task<PaymentDto> RefundAsync(int paymentId)
    {
        var payment = await _repository.GetByIdAsync(paymentId);
        if (payment == null)
            throw new KeyNotFoundException($"Payment {paymentId} not found");

        payment.Status = PaymentStatus.Refunded;
        await _repository.UpdateAsync(payment);

        return MapToDto(payment);
    }

    private PaymentDto MapToDto(PaymentService.Models.Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            BookingId = payment.BookingId,
            Amount = payment.Amount,
            Status = payment.Status.ToString(),
            PaymentMethod = payment.PaymentMethod,
            CreatedAt = payment.CreatedAt
        };
    }
}
