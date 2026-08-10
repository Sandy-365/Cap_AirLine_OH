using Moq;
using PassengerService.DTOs;
using PassengerService.Models;
using PassengerService.Repositories.Interfaces;
using PassengerService.Services.implementations;
using Shared.Security;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace PassengerService.Tests
{
    public class PassengerAuthServiceTests
    {
        private readonly Mock<IPassengerProfileRepository> _repoMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly PassengerAuthService _authService;

        public PassengerAuthServiceTests()
        {
            _repoMock = new Mock<IPassengerProfileRepository>();
            _tokenServiceMock = new Mock<ITokenService>();
            _configMock = new Mock<IConfiguration>();
            
            _authService = new PassengerAuthService(_repoMock.Object, _tokenServiceMock.Object, _configMock.Object);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            var email = "pass@skypass.com";
            var password = "Password123";
            var profile = new PassengerProfile
            {
                Id = 1,
                Email = email,
                Name = "Passenger One",
                PasswordHash = PasswordHasher.Hash(password),
                IsEmailVerified = true,
                IsActive = true,
                Role = "Passenger"
            };

            _repoMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(profile);
            _tokenServiceMock.Setup(t => t.GenerateToken(profile.Id, email, profile.Role)).Returns("mock-token");

            var loginDto = new PassengerLoginDto { Email = email, Password = password };

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("mock-token", result.Token);
            Assert.Equal(profile.Id, result.UserId);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenEmailUnverified()
        {
            // Arrange
            var email = "unverified@skypass.com";
            var profile = new PassengerProfile
            {
                Email = email,
                IsEmailVerified = false // Unverified
            };

            _repoMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(profile);

            var loginDto = new PassengerLoginDto { Email = email, Password = "Any" };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(loginDto));
        }

        [Fact]
        public async Task RegisterAsync_ShouldRefreshProfile_WhenEmailMatchesExistingUnverified()
        {
            // Arrange
            var email = "retry@skypass.com";
            var existingProfile = new PassengerProfile
            {
                Email = email,
                IsEmailVerified = false,
                VerificationToken = "old-token"
            };

            _repoMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(existingProfile);

            var registerDto = new PassengerRegisterDto
            {
                Email = email,
                Name = "New Name",
                Password = "NewPassword123"
            };

            // Act
            await _authService.RegisterAsync(registerDto);

            // Assert
            _repoMock.Verify(r => r.UpdateAsync(It.Is<PassengerProfile>(p => 
                p.Email == email && p.Name == "New Name" && p.VerificationToken != "old-token")), Times.Once);
        }
    }
}
