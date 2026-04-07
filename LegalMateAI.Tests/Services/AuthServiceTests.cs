using LegalMateAI.Tests.Helpers;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.BLL.Services.Service;
using LegalMateAI.DAL.Repositories.IRepository;
using LegalMateAI.Domain.Entities;
using LegalMateAI.DTOs;
using LegalMate.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;
using BCrypt.Net;

namespace LegalMate.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _mockAuthRepo;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _mockAuthRepo = new Mock<IAuthRepository>();
        _mockJwtService = new Mock<IJwtService>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        _service = new AuthService(_mockAuthRepo.Object, _mockJwtService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidUser_ReturnsAuthResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "Test@123"
        };

        var user = TestDataFactory.CreateTestUser(email: request.Email);
        user.PasswordHash =  BCrypt.Net.BCrypt.HashPassword(request.Password);

        _mockAuthRepo.Setup(x => x.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _mockJwtService.Setup(x => x.GenerateToken(user))
            .Returns("fake-jwt-token");

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(request.Email);
        result.Token.Should().Be("fake-jwt-token");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword"
        };

        var user = TestDataFactory.CreateTestUser(email: request.Email);
        user.PasswordHash =  BCrypt.Net.BCrypt.HashPassword("CorrectPassword");

        _mockAuthRepo.Setup(x => x.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "inactive@example.com",
            Password = "Test@123"
        };

        var user = TestDataFactory.CreateTestUser(email: request.Email);
        user.PasswordHash =  BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.IsActive = false;

        _mockAuthRepo.Setup(x => x.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.LoginAsync(request));
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ChangePasswordDto
        {
            CurrentPassword = "OldPass@123",
            NewPassword = "NewPass@123",
            ConfirmPassword = "NewPass@123"
        };

        var user = TestDataFactory.CreateTestUser(id: userId);
        user.PasswordHash =  BCrypt.Net.BCrypt.HashPassword("OldPass@123");

        _mockAuthRepo.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _service.ChangePasswordAsync(userId, request);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPass@123",
            ConfirmPassword = "NewPass@123"
        };

        var user = TestDataFactory.CreateTestUser(id: userId);
        user.PasswordHash =  BCrypt.Net.BCrypt.HashPassword("CorrectPassword");

        _mockAuthRepo.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _service.ChangePasswordAsync(userId, request);

        // Assert
        result.Should().BeFalse();
    }
}