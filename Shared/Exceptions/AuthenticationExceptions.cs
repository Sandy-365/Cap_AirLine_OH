namespace Shared.Exceptions;

// Authentication
public class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message) { }
}

public class InvalidCredentialsException : Exception
{
    public string Email { get; }

    public InvalidCredentialsException(string email)
        : base("Invalid email or password")
    {
        Email = email;
    }
}

public class AccountNotVerifiedException : Exception
{
    public string Email { get; }
    public AccountNotVerifiedException(string email)
        : base($"Account for '{email}' is not verified. Please verify your email first.")
    {
        Email = email;
    }
}

public class AccountDeactivatedException : Exception
{
    public string Email { get; }
    public AccountDeactivatedException(string email)
        : base($"Account for '{email}' has been deactivated. Please contact support.")
    {
        Email = email;
    }
}

public class InvalidTokenException : Exception
{
    public TokenType TokenType { get; }
    public InvalidTokenException(TokenType tokenType, string reason)
        : base($"Invalid {tokenType}: {reason}")
    {
        TokenType = tokenType;
    }
}

public class ExpiredTokenException : Exception
{
    public TokenType TokenType { get; }
    public DateTime ExpiredAt { get; }

    public ExpiredTokenException(TokenType tokenType, DateTime expiredAt)
        : base($"{tokenType} expired at {expiredAt:yyyy-MM-dd HH:mm:ss}")
    {
        TokenType = tokenType;
        ExpiredAt = expiredAt;
    }
}

public enum TokenType
{
    AccessToken,
    RefreshToken,
    VerificationToken,
    PasswordResetToken
}

// OTP
public class InvalidOTPException : Exception
{
    public InvalidOTPException(string message = "Invalid or expired OTP") : base(message) { }
}

public class OTPExpiredException : Exception
{
    public OTPExpiredException() : base("OTP has expired. Please request a new one.") { }
}

public class OTPAttemptsExceededException : Exception
{
    public int MaxAttempts { get; }
    public int AttemptCount { get; }

    public OTPAttemptsExceededException(int maxAttempts, int attempts)
        : base($"Maximum OTP attempts exceeded ({attempts}/{maxAttempts}). Please request a new OTP.")
    {
        MaxAttempts = maxAttempts;
        AttemptCount = attempts;
    }
}

// Authorization
public class AuthorizationException : Exception
{
    public AuthorizationException(string message) : base(message) { }
}

public class InsufficientPermissionsException : Exception
{
    public string RequiredRole { get; }
    public string UserRoles { get; }

    public InsufficientPermissionsException(string requiredRole, string userRoles)
        : base($"Insufficient permissions. Required: {requiredRole}, User roles: {userRoles}")
    {
        RequiredRole = requiredRole;
        UserRoles = userRoles;
    }
}

public class EmailAlreadyRegisteredException : Exception
{
    public string Email { get; }
    public EmailAlreadyRegisteredException(string email)
        : base($"Email '{email}' is already registered")
    {
        Email = email;
    }
}

public class UserNotFoundException : Exception
{
    public int UserId { get; }
    public string? Email { get; }

    public UserNotFoundException(int userId) : base($"User {userId} not found")
    {
        UserId = userId;
    }

    public UserNotFoundException(string email) : base($"User with email '{email}' not found")
    {
        Email = email;
    }
}
