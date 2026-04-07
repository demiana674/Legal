using LegalMateAI.BLL.Services.IService;
using LegalMateAI.BLL.Services.Service;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace LegalMate.Tests.Services;

public class CaseServiceTests : IDisposable
{
    private readonly LegalMateDbContext _context;
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<CaseService>> _mockLogger;
    private readonly CaseService _service;
    private readonly User _testClient;
    private readonly User _testLawyer;
    private readonly LawyerProfile _lawyerProfile;

    public CaseServiceTests()
    {
        var options = new DbContextOptionsBuilder<LegalMateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new LegalMateDbContext(options);
        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<CaseService>>();
        _service = new CaseService(_context, _mockEnv.Object, _mockHttpContextAccessor.Object, _mockLogger.Object);

        // Seed test data
        _testClient = TestDataFactory.CreateTestUser(role: UserRole.User);
        _context.Users.Add(_testClient);
        
        _testLawyer = TestDataFactory.CreateTestUser(role: UserRole.Lawyer);
        _context.Users.Add(_testLawyer);
        
        _lawyerProfile = TestDataFactory.CreateTestLawyerProfile(_testLawyer.UserID);
        _context.LawyerProfiles.Add(_lawyerProfile);
        
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateCaseAsync_ValidRequest_ReturnsCase()
    {
        // Arrange
        var request = new CreateCaseDto
        {
            Title = "نزاع ملكية عقارية",
            Description = "خلاف على ملكية عقار في مدينة نصر",
            ClientId = _testClient.UserID,
            Court = "محكمة القاهرة الابتدائية",
            NextHearingDate = DateTime.UtcNow.AddDays(14),
            Status = CaseStatus.Active,
            Priority = CasePriority.High,
            CaseType = "مدني"
        };

        // Act
        var result = await _service.CreateCaseAsync(_testLawyer.UserID, request, isLawyer: true);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("نزاع ملكية عقارية");
        result.Status.Should().Be(CaseStatus.Active);
        result.Priority.Should().Be(CasePriority.High);
        result.CaseNumber.Should().StartWith("CS-");
    }

    [Fact]
    public async Task GetCasesAsync_WithClientFilter_ReturnsClientCases()
    {
        // Arrange
        var testCase = TestDataFactory.CreateTestCase(_testClient.UserID);
        _context.Cases.Add(testCase);
        await _context.SaveChangesAsync();

        var filter = new CaseFilterDto
        {
            ClientId = _testClient.UserID
        };

        // Act
        var result = await _service.GetCasesAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result.First().ClientId.Should().Be(_testClient.UserID);
    }

    [Fact]
    public async Task UpdateCaseAsync_ValidUpdate_ReturnsUpdatedCase()
    {
        // Arrange
        var testCase = TestDataFactory.CreateTestCase(_testClient.UserID, _testLawyer.UserID);
        _context.Cases.Add(testCase);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateCaseDto
        {
            Title = "قضية محدثة - نزاع تجاري",
            Status = CaseStatus.Completed,
            Priority = CasePriority.Urgent
        };

        // Act
        var result = await _service.UpdateCaseAsync(_testLawyer.UserID, testCase.Id, updateRequest, isLawyer: true);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("قضية محدثة - نزاع تجاري");
        result.Status.Should().Be(CaseStatus.Completed);
        result.Priority.Should().Be(CasePriority.Urgent);
    }

    [Fact]
    public async Task AddNoteAsync_ValidNote_AddsNoteToCase()
    {
        // Arrange
        var testCase = TestDataFactory.CreateTestCase(_testClient.UserID, _testLawyer.UserID);
        _context.Cases.Add(testCase);
        await _context.SaveChangesAsync();

        var request = new CreateCaseNoteDto
        {
            CaseId = testCase.Id,
            Content = "تم استلام المستندات المطلوبة من الموكل",
            IsPrivate = false
        };

        // Act
        var result = await _service.AddNoteAsync(_testLawyer.UserID, request, isLawyer: true);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Be("تم استلام المستندات المطلوبة من الموكل");
        result.IsPrivate.Should().BeFalse();
    }

    [Fact]
    public async Task GetCaseStatsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        _context.Cases.Add(TestDataFactory.CreateTestCase(_testClient.UserID, _testLawyer.UserID));
        _context.Cases.Add(TestDataFactory.CreateTestCase(_testClient.UserID, _testLawyer.UserID));
        _context.Cases.Add(TestDataFactory.CreateTestCase(_testClient.UserID, _testLawyer.UserID));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCaseStatsAsync(lawyerId: _testLawyer.UserID);

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(3);
        result.Active.Should().Be(3);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}