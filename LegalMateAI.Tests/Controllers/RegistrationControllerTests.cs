using LegalMateAI.Tests.Helpers;
using LegalMateAI.API.Controllers;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.Infrastructure.Services.IService;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace LegalMate.Tests.Controllers;

public class RegistrationControllerTests
{
    private readonly Mock<IRegistrationService> _mockRegService;
    private readonly Mock<IEncryptionService> _mockEncryption;
    private readonly Mock<ILogger<RegistrationController>> _mockLogger;
    private readonly RegistrationController _controller;

    public RegistrationControllerTests()
    {
        _mockRegService = new Mock<IRegistrationService>();
        _mockEncryption = new Mock<IEncryptionService>();
        _mockLogger = new Mock<ILogger<RegistrationController>>();
        _controller = new RegistrationController(_mockRegService.Object, _mockEncryption.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Register_ValidUser_ReturnsOk()
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

        var expectedResult = new RegistrationResult
        {
            Success = true,
            Message = "تم تسجيل المستخدم بنجاح",
            UserId = Guid.NewGuid(),
            RequiresApproval = false
        };

        _mockRegService.Setup(x => x.RegisterAsync(request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Register_InvalidModel_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Email", "Email is required");
        var request = new RegisterRequest();

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Register_ExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456",
            Role = UserRole.User
        };

        var expectedResult = new RegistrationResult
        {
            Success = false,
            Message = "البريد الإلكتروني موجود بالفعل"
        };

        _mockRegService.Setup(x => x.RegisterAsync(request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }
}