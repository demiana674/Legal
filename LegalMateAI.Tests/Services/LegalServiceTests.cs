using LegalMateAI.Tests.Helpers;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.BLL.Services.Service;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMate.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace LegalMate.Tests.Services;

public class LegalServiceTests : IDisposable
{
    private readonly LegalMateDbContext _context;
    private readonly Mock<IAIService> _mockAiService;
    private readonly Mock<ILogger<LegalService>> _mockLogger;
    private readonly LegalService _service;

    public LegalServiceTests()
    {
        var options = new DbContextOptionsBuilder<LegalMateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new LegalMateDbContext(options);
        _mockAiService = new Mock<IAIService>();
        _mockLogger = new Mock<ILogger<LegalService>>();
        _service = new LegalService(_context, _mockAiService.Object, _mockLogger.Object);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        var law1 = TestDataFactory.CreateTestLaw();
        var law2 = new EgyptianLaw
        {
            Id = 2,
            LawNumber = "قانون 58 لسنة 1937",
            TitleAr = "قانون العقوبات",
            Year = 1937,
            Category = LawCategory.Criminal,
            Status = LawStatus.Active,
            PublishedAt = new DateTime(1937, 8, 5)
        };

        _context.EgyptianLaws.AddRange(law1, law2);
        
        var article = TestDataFactory.CreateTestArticle(1, 1);
        _context.LawArticles.Add(article);
        
        _context.SaveChanges();
    }

    [Fact]
    public async Task SearchLawsAsync_ValidQuery_ReturnsResults()
    {
        // Arrange
        var query = "مدني";

        // Act
        var result = await _service.SearchLawsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalResults.Should().BeGreaterThan(0);
        result.Results.Should().Contain(r => r.Title.Contains("مدني"));
    }

    [Fact]
    public async Task GetLawByIdAsync_ExistingLaw_ReturnsLaw()
    {
        // Act
        var result = await _service.GetLawByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.LawNumber.Should().Be("قانون 131 لسنة 1948");
        result.TitleAr.Should().Be("القانون المدني");
        result.Category.Should().Be(LawCategory.Civil);
    }

    [Fact]
    public async Task GetLawByIdAsync_NonExistingLaw_ReturnsNull()
    {
        // Act
        var result = await _service.GetLawByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllLawsAsync_ReturnsAllLaws()
    {
        // Act
        var result = await _service.GetAllLawsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetAllLawsAsync_WithCategoryFilter_ReturnsFilteredLaws()
    {
        // Act
        var result = await _service.GetAllLawsAsync(LawCategory.Civil);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result.First().Category.Should().Be(LawCategory.Civil);
    }

    [Fact]
    public async Task GetArticleByIdAsync_ExistingArticle_ReturnsArticle()
    {
        // Act
        var result = await _service.GetArticleByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.ArticleNumber.Should().Be(1);
        result.Content.Should().NotBeNullOrEmpty();
        result.LawId.Should().Be(1);
    }

    [Fact]
    public async Task GetArticleByIdAsync_NonExistingArticle_ReturnsNull()
    {
        // Act
        var result = await _service.GetArticleByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLawAmendmentsAsync_ReturnsAmendments()
    {
        // Act
        var result = await _service.GetLawAmendmentsAsync(1);

        // Assert
        result.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}