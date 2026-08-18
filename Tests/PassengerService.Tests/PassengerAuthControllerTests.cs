using Microsoft.AspNetCore.Mvc;
using Moq;
using PassengerService.Controllers;
using PassengerService.DTOs;
using PassengerService.Services.Interfaces;
using Xunit;

namespace PassengerService.Tests;

public class PassengerAuthControllerTests
{
    private readonly Mock<IPassengerAuthService> _authServiceMock;
    private readonly PassengerAuthController _controller;

    public PassengerAuthControllerTests()
    {
        _authServiceMock = new Mock<IPassengerAuthService>();
        _controller = new PassengerAuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkResultWithToken()
    {
        // Arrange
        var loginDto = new PassengerLoginDto
        {
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        var expectedResponse = new PassengerAuthResponseDto
        {
            UserId = 1,
            Email = "john.doe@example.com",
            Name = "John Doe",
            Role = "Passenger",
            Token = "fake-jwt-token-12345"
        };

        _authServiceMock
            .Setup(s => s.LoginAsync(loginDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var actualResponse = Assert.IsType<PassengerAuthResponseDto>(okResult.Value);
        Assert.Equal(expectedResponse.Email, actualResponse.Email);
        Assert.Equal(expectedResponse.Token, actualResponse.Token);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new PassengerLoginDto
        {
            Email = "invalid@example.com",
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
    public async Task Register_WithValidDto_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        var registerDto = new PassengerRegisterDto
        {
            Name = "Jane Doe",
            Email = "jane.doe@example.com",
            Password = "SecurePassword123!"
        };

        _authServiceMock
            .Setup(s => s.RegisterAsync(registerDto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _authServiceMock.Verify(s => s.RegisterAsync(registerDto), Times.Once);
    }

    [Fact]
    public async Task Verify_WithInvalidOtp_ReturnsBadRequest()
    {
        // Arrange
        var verifyDto = new PassengerVerifyDto
        {
            Email = "test@example.com",
            Token = "999999"
        };

        _authServiceMock
            .Setup(s => s.VerifyAsync(verifyDto))
            .ThrowsAsync(new InvalidOperationException("Invalid or expired OTP."));

        // Act
        var result = await _controller.Verify(verifyDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }
}
