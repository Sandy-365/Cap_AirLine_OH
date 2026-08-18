using BackOfficeService.Controllers;
using BackOfficeService.DTOs;
using BackOfficeService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BackOfficeService.Tests;

public class BackofficeAuthControllerTests
{
    private readonly Mock<IBackofficeAuthService> _authServiceMock;
    private readonly BackofficeAuthController _controller;

    public BackofficeAuthControllerTests()
    {
        _authServiceMock = new Mock<IBackofficeAuthService>();
        _controller = new BackofficeAuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkResultWithToken()
    {
        // Arrange
        var loginDto = new BackofficeLoginDto
        {
            Email = "admin@airline.com",
            Password = "AdminPassword123!"
        };

        var expectedResponse = new BackofficeAuthResponseDto
        {
            UserId = 1,
            Email = "admin@airline.com",
            Name = "System Admin",
            Role = "Admin",
            Token = "fake-jwt-backoffice-token-12345"
        };

        _authServiceMock
            .Setup(s => s.LoginAsync(loginDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var actualResponse = Assert.IsType<BackofficeAuthResponseDto>(okResult.Value);
        Assert.Equal(expectedResponse.Email, actualResponse.Email);
        Assert.Equal(expectedResponse.Role, actualResponse.Role);
        Assert.Equal(expectedResponse.Token, actualResponse.Token);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new BackofficeLoginDto
        {
            Email = "admin@airline.com",
            Password = "WrongPassword"
        };

        _authServiceMock
            .Setup(s => s.LoginAsync(loginDto))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password."));

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var resetDto = new BackofficeResetPasswordDto
        {
            Email = "admin@airline.com",
            Token = "000000",
            NewPassword = "NewSecurePassword123!"
        };

        _authServiceMock
            .Setup(s => s.ResetPasswordAsync(resetDto))
            .ThrowsAsync(new InvalidOperationException("Invalid or expired OTP token."));

        // Act
        var result = await _controller.ResetPassword(resetDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }
}
