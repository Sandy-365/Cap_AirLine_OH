namespace Shared.Exceptions;

// Payment Processing
public class PaymentProcessingException : Exception
{
    public int BookingId { get; }
    public decimal Amount { get; }
    public string? PaymentGatewayResponse { get; }

    public PaymentProcessingException(int bookingId, decimal amount, string message)
        : base($"Payment processing failed for booking {bookingId}, amount: {amount:C}. {message}")
    {
        BookingId = bookingId;
        Amount = amount;
    }

    public PaymentProcessingException(int bookingId, decimal amount, string message, Exception inner)
        : base($"Payment processing failed for booking {bookingId}, amount: {amount:C}. {message}", inner) { }
}

public class PaymentGatewayException : Exception
{
    public string? GatewayCode { get; }
    public PaymentGatewayException(string message, string? gatewayCode = null) 
        : base($"Payment gateway error: {message}")
    {
        GatewayCode = gatewayCode;
    }
}

public class PaymentTimeoutException : Exception
{
    public int BookingId { get; }
    public PaymentTimeoutException(int bookingId)
        : base($"Payment timeout for booking {bookingId}")
    {
        BookingId = bookingId;
    }
}

// Payment State
public class PaymentAlreadyProcessedException : Exception
{
    public string PaymentId { get; }
    public PaymentAlreadyProcessedException(string paymentId)
        : base($"Payment {paymentId} has already been processed")
    {
        PaymentId = paymentId;
    }
}

public class PaymentNotFoundException : Exception
{
    public string PaymentId { get; }
    public PaymentNotFoundException(string paymentId) : base($"Payment {paymentId} not found")
    {
        PaymentId = paymentId;
    }
}

public class InvalidPaymentAmountException : Exception
{
    public decimal RequestedAmount { get; }
    public decimal ExpectedAmount { get; }

    public InvalidPaymentAmountException(decimal requested, decimal expected)
        : base($"Invalid payment amount. Requested: {requested:C}, Expected: {expected:C}")
    {
        RequestedAmount = requested;
        ExpectedAmount = expected;
    }
}

// Refund
public class RefundNotAllowedException : Exception
{
    public int BookingId { get; }
    public string Reason { get; }

    public RefundNotAllowedException(int bookingId, string reason)
        : base($"Refund not allowed for booking {bookingId}: {reason}")
    {
        BookingId = bookingId;
        Reason = reason;
    }
}

public class InsufficientRefundBalanceException : Exception
{
    public decimal RequestedRefund { get; }
    public decimal AvailableBalance { get; }

    public InsufficientRefundBalanceException(decimal requested, decimal available)
        : base($"Insufficient balance for refund. Requested: {requested:C}, Available: {available:C}")
    {
        RequestedRefund = requested;
        AvailableBalance = available;
    }
}
