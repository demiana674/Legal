// LegalMateAI.DAL/SeedData/AdminSeedData.cs
using LegalMateAI.Domain.Entities;
using LegalMateAI.Infrastructure.Services.IService;
using Microsoft.Extensions.Configuration;

namespace LegalMateAI.DAL.SeedData
{
    public static class AdminSeedData
    {
        public static List<Admin> GetDefaultAdmins(IConfiguration config, IEncryptionService encryption)
        {
            var admins = new List<Admin>();
            
            // قراءة إعدادات الأدمن من appsettings.json
            var defaultAdmin = config.GetSection("AdminAccounts:DefaultAdmin");
            var verifier = config.GetSection("AdminAccounts:Verifier");
            
            // الأدمن الرئيسي
            admins.Add(new Admin
            {
                Id = Guid.NewGuid(),
                FullName = defaultAdmin["FullName"] ?? "مدير النظام",
                Email = defaultAdmin["Email"] ?? "admin@legalmate.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultAdmin["Password"] ?? "Admin@123"),
                PhoneNumber = encryption.Encrypt(defaultAdmin["PhoneNumber"] ?? "01000000000"),
                CreatedAt = DateTime.UtcNow
            });
            
            // مدقق المحامين
            admins.Add(new Admin
            {
                Id = Guid.NewGuid(),
                FullName = verifier["FullName"] ?? "مدقق المحامين",
                Email = verifier["Email"] ?? "verifier@legalmate.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(verifier["Password"] ?? "Verifier@123"),
                PhoneNumber = encryption.Encrypt(verifier["PhoneNumber"] ?? "01000000001"),
                CreatedAt = DateTime.UtcNow
            });
            
            return admins;
        }
    }
}