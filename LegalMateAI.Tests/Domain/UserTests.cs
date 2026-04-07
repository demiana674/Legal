using LegalMateAI.Tests.Helpers;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMate.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace LegalMate.Tests.Domain;

public class UserTests
{
    [Fact]
    public void User_Create_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var user = TestDataFactory.CreateTestUser();

        // Assert
        user.Should().NotBeNull();
        user.UserID.Should().NotBeEmpty();
        user.FirstName.Should().NotBeNullOrEmpty();
        user.LastName.Should().NotBeNullOrEmpty();
        user.FullName.Should().Be($"{user.FirstName} {user.LastName}");
        user.Email.Should().NotBeNullOrEmpty();
        user.Role.Should().Be(UserRole.User);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void User_FullName_ShouldCombineFirstAndLastName()
    {
        // Arrange
        var user = TestDataFactory.CreateTestUser();
        user.FirstName = "Ahmed";
        user.LastName = "Mohamed";

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().Be("Ahmed Mohamed");
    }

    [Fact]
    public void User_WithLawyerRole_ShouldHaveLawyerProfile()
    {
        // Arrange
        var user = TestDataFactory.CreateTestUser(role: UserRole.Lawyer);
        var lawyerProfile = TestDataFactory.CreateTestLawyerProfile(user.UserID);

        // Act
        user.LawyerProfile = lawyerProfile;

        // Assert
        user.LawyerProfile.Should().NotBeNull();
        user.LawyerProfile.LicenseNumber.Should().NotBeNullOrEmpty();
        user.Role.Should().Be(UserRole.Lawyer);
    }
}