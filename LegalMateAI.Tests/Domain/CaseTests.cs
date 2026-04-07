using LegalMateAI.Tests.Helpers;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMate.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace LegalMate.Tests.Domain;

public class CaseTests
{
    [Fact]
    public void Case_Create_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var lawyerId = Guid.NewGuid();

        // Act
        var testCase = TestDataFactory.CreateTestCase(clientId, lawyerId);

        // Assert
        testCase.Should().NotBeNull();
        testCase.Id.Should().NotBeEmpty();
        testCase.CaseNumber.Should().StartWith("CS-");
        testCase.ClientId.Should().Be(clientId);
        testCase.LawyerId.Should().Be(lawyerId);
        testCase.Status.Should().Be(CaseStatus.Active);
        testCase.Priority.Should().Be(CasePriority.High);
    }

    [Fact]
    public void Case_Complete_ShouldChangeStatusToCompleted()
    {
        // Arrange
        var testCase = TestDataFactory.CreateTestCase(Guid.NewGuid());
        testCase.Status = CaseStatus.Active;

        // Act
        testCase.Status = CaseStatus.Completed;
        testCase.ClosedAt = DateTime.UtcNow;

        // Assert
        testCase.Status.Should().Be(CaseStatus.Completed);
        testCase.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void Case_AddDocument_ShouldAddToDocumentsCollection()
    {
        // Arrange
        var testCase = TestDataFactory.CreateTestCase(Guid.NewGuid());
        var document = TestDataFactory.CreateTestCaseDocument(testCase.Id, Guid.NewGuid());

        // Act
        testCase.Documents.Add(document);

        // Assert
        testCase.Documents.Should().ContainSingle();
        testCase.Documents.First().FileName.Should().Be("case_document.pdf");
    }

    [Fact]
    public void Case_AddNote_ShouldAddToNotesCollection()
    {
        // Arrange
        var testCase = TestDataFactory.CreateTestCase(Guid.NewGuid());
        var note = TestDataFactory.CreateTestCaseNote(testCase.Id, Guid.NewGuid());

        // Act
        testCase.Notes.Add(note);

        // Assert
        testCase.Notes.Should().ContainSingle();
        testCase.Notes.First().Content.Should().NotBeNullOrEmpty();
    }
}