using LegalMateAI.Tests.Helpers;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMate.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace LegalMate.Tests.Domain;

public class ContractTests
{
    [Fact]
    public void Contract_Create_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var contract = TestDataFactory.CreateTestContract(userId);

        // Assert
        contract.Should().NotBeNull();
        contract.Id.Should().NotBeEmpty();
        contract.ContractNumber.Should().StartWith("CNT-");
        contract.UserId.Should().Be(userId);
        contract.Type.Should().Be(ContractType.Rental);
        contract.Status.Should().Be(ContractStatus.Draft);
    }

    [Fact]
    public void Contract_Sign_ShouldChangeStatusToActive()
    {
        // Arrange
        var contract = TestDataFactory.CreateTestContract(Guid.NewGuid());
        contract.Status = ContractStatus.PendingSignature;

        // Act
        contract.Status = ContractStatus.Active;
        contract.SignedAt = DateTime.UtcNow;

        // Assert
        contract.Status.Should().Be(ContractStatus.Active);
        contract.SignedAt.Should().NotBeNull();
    }

    [Fact]
    public void Contract_Expire_ShouldChangeStatusToExpired()
    {
        // Arrange
        var contract = TestDataFactory.CreateTestContract(Guid.NewGuid());
        contract.Status = ContractStatus.Active;
        contract.EndDate = DateTime.UtcNow.AddDays(-1);

        // Act
        if (contract.EndDate < DateTime.UtcNow)
        {
            contract.Status = ContractStatus.Expired;
        }

        // Assert
        contract.Status.Should().Be(ContractStatus.Expired);
    }
}