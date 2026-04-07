using LegalMateAI.DAL.DBContext;
using Microsoft.EntityFrameworkCore;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.Tests.Helpers;
namespace LegalMate.Tests.Helpers;

public static class MockDbContextFactory
{
    public static LegalMateDbContext CreateInMemoryDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<LegalMateDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new LegalMateDbContext(options);
    }

    public static async Task<LegalMateDbContext> CreateSeededDbContext()
    {
        var context = CreateInMemoryDbContext();
        
        // Seed Users
        var user1 = TestDataFactory.CreateTestUser(role: UserRole.User);
        var user2 = TestDataFactory.CreateTestUser(role: UserRole.User);
        var lawyerUser = TestDataFactory.CreateTestUser(role: UserRole.Lawyer);
        
        context.Users.AddRange(user1, user2, lawyerUser);
        
        // Seed Lawyer Profile
        var lawyerProfile = TestDataFactory.CreateTestLawyerProfile(lawyerUser.UserID);
        context.LawyerProfiles.Add(lawyerProfile);
        
        // Seed Laws
        context.EgyptianLaws.Add(TestDataFactory.CreateTestLaw());
        
        await context.SaveChangesAsync();
        
        return context;
    }
}