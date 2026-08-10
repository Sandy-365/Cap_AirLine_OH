using AdminService.Data;
using AdminService.DTOs;
using AdminService.Models;
using AdminService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Shared.Security;
using Xunit;

namespace AdminService.Tests
{
    public class AdminAuthServiceTests
    {
        private readonly AdminDbContext _db;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly AdminAuthService _authService;

        public AdminAuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<AdminDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _db = new AdminDbContext(options);
            _tokenServiceMock = new Mock<ITokenService>();
            _configMock = new Mock<IConfiguration>();
            
            _authService = new AdminAuthService(_db, _tokenServiceMock.Object, _configMock.Object);
        }
        ///<summary>
        /// Test case for RegisterAsync method
        ///</summary>

        [Fact]
        public async Task RegisterAsync_ShouldCreateNewProfile_WhenEmailIsNew()
        {
            // Arrange
            var dto = new AdminRegisterDto
            {
                Email = "newadmin@skypass.com",
                Name = "New Admin",
                Password = "Password123",
                Role = "Admin",
                ProvisionedByAdmin = true
            };

            // Act
            await _authService.RegisterAsync(dto);

            // Assert
            var profile = await _db.AdminProfiles.FirstOrDefaultAsync(p => p.Email == dto.Email);
            Assert.NotNull(profile);
            Assert.Equal("New Admin", profile.Name);
            Assert.True(profile.IsEmailVerified);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowException_WhenVerifiedEmailAlreadyExists()
        {
            // Arrange
            var email = "existing@skypass.com";
            var existingProfile = new AdminProfile
            {
                Email = email,
                Name = "Existing",
                PasswordHash = "hashed",
                IsEmailVerified = true,
                Role = "Admin"
            };
            await _db.AdminProfiles.AddAsync(existingProfile);
            await _db.SaveChangesAsync();

            var dto = new AdminRegisterDto
            {
                Email = email,
                Name = "New Profile Attempt",
                Password = "Password123"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            var email = "login@skypass.com";
            var password = "Password123";
            var profile = new AdminProfile
            {
                Id = Guid.NewGuid(),
                Email = email,
                Name = "Login User",
                PasswordHash = PasswordHasher.Hash(password),
                IsEmailVerified = true,
                IsActive = true,
                Role = "Admin"
            };
            await _db.AdminProfiles.AddAsync(profile);
            await _db.SaveChangesAsync();

            _tokenServiceMock.Setup(s => s.GenerateToken(profile.Id, profile.Email, profile.Role))
                .Returns("mock-token");

            var loginDto = new AdminLoginDto { Email = email, Password = password };

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("mock-token", result.Token);
            Assert.Equal(profile.Id, result.UserId);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenPasswordIsIncorrect()
        {
            // Arrange
            var email = "wrongpass@skypass.com";
            var correctPassword = "CorrectPassword";
            var profile = new AdminProfile
            {
                Email = email,
                PasswordHash = PasswordHasher.Hash(correctPassword),
                IsEmailVerified = true,
                IsActive = true,
                Role = "Admin"
            };
            await _db.AdminProfiles.AddAsync(profile);
            await _db.SaveChangesAsync();

            var loginDto = new AdminLoginDto { Email = email, Password = "WrongPassword" };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(loginDto));
        }
    }
}
