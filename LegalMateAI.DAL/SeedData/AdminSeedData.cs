using LegalMateAI.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using LegalMateAI.Infrastructure.Services.IService;

namespace LegalMateAI.DAL.SeedData
{
    public static class AdminSeedData
    {
        public static List<Admin> GetDefaultAdmins(IConfiguration config, IEncryptionService encryption)
        {
            return new List<Admin>
            {
                new Admin
                {
                    Id = Guid.NewGuid(),
                    FullName = "مدير النظام",
                    Email = "admin@legalmate.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    PhoneNumber = encryption.Encrypt("01000000000"),
                    CreatedAt = DateTime.UtcNow
                },
                new Admin
                {
                    Id = Guid.NewGuid(),
                    FullName = "مدقق المحامين",
                    Email = "verifier@legalmate.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Verifier@123"),
                    PhoneNumber = encryption.Encrypt("01000000001"),
                    CreatedAt = DateTime.UtcNow
                }
            };
        }
    }
}
