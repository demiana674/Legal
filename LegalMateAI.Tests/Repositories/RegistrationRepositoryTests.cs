using LegalMateAI.Tests.Helpers;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.DAL.Repositories.Repository;
using LegalMateAI.Domain.Entities;
using LegalMate.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LegalMate.Tests.Repositories;

public class RegistrationRepositoryTests : IDisposable
{
    private readonly LegalMateDbContext _context;
    private readonly RegistrationRepository _repository;

    public RegistrationRepositoryTests()
    {
        _context = MockDbContextFactory.CreateInMemoryDbContext();
        _repository = new RegistrationRepository(_context);
    }

    [Fact]
    public async Task UserExistsAsync_ExistingUser_ReturnsTrue()
    {
        // Arrange
        var user = TestDataFactory.CreateTestUser(email: "existing@example.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.UserExistsAsync("existing@example.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task NationalIdExistsAsync_ExistingNationalId_ReturnsTrue()
    {
        // Arrange
        var user = TestDataFactory.CreateTestUser();
        user.NationalId = "12345678901234";
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.NationalIdExistsAsync("12345678901234");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AddUserWithProfileAsync_ShouldAddBothUserAndProfile()
    {
        // Arrange
        var user = TestDataFactory.CreateTestUser();
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.UserID,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email
        };

        // Act
        await _repository.AddUserWithProfileAsync(user, profile);
        await _repository.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FindAsync(user.UserID);
        var savedProfile = await _context.UserProfiles.FindAsync(profile.Id);
        
        savedUser.Should().NotBeNull();
        savedProfile.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}