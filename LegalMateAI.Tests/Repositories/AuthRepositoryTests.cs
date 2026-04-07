using LegalMateAI.Tests.Helpers;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.DAL.Repositories.Repository;
using LegalMateAI.Domain.Entities;
using LegalMate.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LegalMate.Tests.Repositories;

public class AuthRepositoryTests : IDisposable
{
    private readonly LegalMateDbContext _context;
    private readonly AuthRepository _repository;

    public AuthRepositoryTests()
    {
        _context = MockDbContextFactory.CreateInMemoryDbContext();
        _repository = new AuthRepository(_context);
    }

    [Fact]
    public async Task GetUserByEmailAsync_ExistingEmail_ReturnsUser()
    {
        // Arrange
        var user = TestDataFactory.CreateTestUser(email: "test@example.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUserByEmailAsync("test@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetUserByEmailAsync_NonExistingEmail_ReturnsNull()
    {
        // Act
        var result = await _repository.GetUserByEmailAsync("notfound@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddUserAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        var user = TestDataFactory.CreateTestUser();

        // Act
        await _repository.AddUserAsync(user);
        await _repository.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        var user = TestDataFactory.CreateTestUser(email: "exists@example.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.EmailExistsAsync("exists@example.com");

        // Assert
        result.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}