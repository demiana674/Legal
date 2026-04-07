using LegalMateAI.Tests.Helpers;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.BLL.Services.Service;
using LegalMateAI.DAL.Repositories.IRepository;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs;
using LegalMate.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;
using LegalMateAI.Infrastructure.Services.IService;
namespace LegalMate.Tests.Services;

public class RegistrationServiceTests
{
    private readonly Mock<IRegistrationRepository> _mockRepo;
    private readonly Mock<IEncryptionService> _mockEncryption;
    private readonly Mock<ILogger<RegistrationService>> _mockLogger;
    private readonly RegistrationService _service;

    public RegistrationServiceTests()
    {
        _mockRepo = new Mock<IRegistrationRepository>();
        _mockEncryption = new Mock<IEncryptionService>();
        _mockLogger = new Mock<ILogger<RegistrationService>>();
        _service = new RegistrationService(_mockRepo.Object, _mockEncryption.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidUser_ReturnsSuccess()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FirstName = "Ahmed",
            LastName = "Mohamed",
            Email = "ahmed@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456",
            Phone = "01000000000",
            NationalId = "12345678901234",
            Role = UserRole.User
        };

        _mockRepo.Setup(x => x.UserExistsAsync(request.Email))
            .ReturnsAsync(false);
        _mockRepo.Setup(x => x.NationalIdExistsAsync(request.NationalId))
            .ReturnsAsync(false);

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("تم تسجيل المستخدم بنجاح");
        result.RequiresApproval.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456",
            Role = UserRole.User
        };

        _mockRepo.Setup(x => x.UserExistsAsync(request.Email))
            .ReturnsAsync(true);

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("البريد الإلكتروني موجود بالفعل");
    }

    [Fact]
    public async Task RegisterAsync_ValidLawyer_ReturnsSuccessWithApproval()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FirstName = "Karim",
            LastName = "Mahmoud",
            Email = "karim@law.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456",
            Phone = "01000000000",
            NationalId = "12345678901234",
            Role = UserRole.Lawyer,
            LicenseNumber = "LAW-12345",
            BarAssociation = "نقابة المحامين"
        };

        _mockRepo.Setup(x => x.UserExistsAsync(request.Email))
            .ReturnsAsync(false);
        _mockRepo.Setup(x => x.NationalIdExistsAsync(request.NationalId))
            .ReturnsAsync(false);
        _mockRepo.Setup(x => x.LicenseNumberExistsAsync(request.LicenseNumber))
            .ReturnsAsync(false);

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("تم تقديم طلب تسجيل المحامي بنجاح، في انتظار موافقة الإدارة");
        result.RequiresApproval.Should().BeTrue();
    }
}