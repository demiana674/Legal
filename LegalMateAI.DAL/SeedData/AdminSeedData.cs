using LegalMateAI.Domain.Entities;
using LegalMateAI.Infrastructure.Services.IService;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LegalMateAI.DAL.DBContext;

namespace LegalMateAI.DAL.SeedData
{
    public static class AdminSeedData
    {
        public static async Task SeedAdminsAsync(LegalMateDbContext context, IConfiguration config, IEncryptionService encryption, ILogger logger)
        {
            logger.LogInformation("🌱 Seeding Admins...");

            var existingCount = await context.Admins.CountAsync();
            if (existingCount > 0)
            {
                logger.LogInformation($"⏭️ Admins already exist ({existingCount} records), skipping...");
                return;
            }

            var admins = GetDefaultAdmins(config, encryption);
            var addedAdmins = new List<Admin>();

            foreach (var admin in admins)
            {
                var existing = await context.Admins.FirstOrDefaultAsync(a => a.Email == admin.Email);
                if (existing == null)
                {
                    await context.Admins.AddAsync(admin);
                    addedAdmins.Add(admin);
                    logger.LogInformation($"✅ Added admin: {admin.FullName}");
                }
                else
                {
                    addedAdmins.Add(existing);
                    logger.LogInformation($"ℹ️ Admin already exists: {admin.FullName}");
                }
            }

            await context.SaveChangesAsync();
            logger.LogInformation($"✅ Seeded {addedAdmins.Count} admins");

            await SeedAdminProfilesAsync(context, config, encryption, logger, addedAdmins);
        }

        private static async Task SeedAdminProfilesAsync(LegalMateDbContext context, IConfiguration config, IEncryptionService encryption, ILogger logger, List<Admin> admins)
        {
            logger.LogInformation("🌱 Seeding Admin Profiles...");

            var existingProfilesCount = await context.AdminProfiles.CountAsync();
            if (existingProfilesCount > 0)
            {
                logger.LogInformation($"⏭️ Admin profiles already exist ({existingProfilesCount} records), skipping...");
                return;
            }

            var profiles = GetDefaultAdminProfiles(config, encryption, admins);

            foreach (var profile in profiles)
            {
                var existing = await context.AdminProfiles.FirstOrDefaultAsync(p => p.AdminId == profile.AdminId);
                if (existing == null)
                {
                    await context.AdminProfiles.AddAsync(profile);
                    logger.LogInformation($"✅ Added admin profile for: {profile.FirstName} {profile.LastName}");
                }
            }

            await context.SaveChangesAsync();
            logger.LogInformation($"✅ Seeded {profiles.Count} admin profiles");
        }

        public static List<Admin> GetDefaultAdmins(IConfiguration config, IEncryptionService encryption)
        {
            var admins = new List<Admin>();
            var defaultAdmin = config.GetSection("AdminAccounts:DefaultAdmin");
            var verifier = config.GetSection("AdminAccounts:Verifier");

            admins.Add(new Admin
            {
                Id = Guid.NewGuid(),
                FullName = defaultAdmin["FullName"] ?? "سامي عبدالعزيز محمود",
                Email = defaultAdmin["Email"] ?? "admin@legalmate.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultAdmin["Password"] ?? "Admin@123"),
                PhoneNumber = encryption.Encrypt(defaultAdmin["PhoneNumber"] ?? "+20 100 987 6543"),
                CreatedAt = DateTime.TryParse(defaultAdmin["JoinDate"], out var d1) ? d1 : new DateTime(2022, 3, 1)
            });

            admins.Add(new Admin
            {
                Id = Guid.NewGuid(),
                FullName = verifier["FullName"] ?? "أحمد رضا الشافعي",
                Email = verifier["Email"] ?? "verifier@legalmate.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(verifier["Password"] ?? "Verifier@123"),
                PhoneNumber = encryption.Encrypt(verifier["PhoneNumber"] ?? "+20 100 111 2222"),
                CreatedAt = DateTime.TryParse(verifier["JoinDate"], out var d2) ? d2 : new DateTime(2023, 6, 15)
            });

            return admins;
        }

        public static List<AdminProfile> GetDefaultAdminProfiles(
            IConfiguration config, IEncryptionService encryption, List<Admin> admins)
        {
            var profiles = new List<AdminProfile>();
            var defaultAdminSection = config.GetSection("AdminAccounts:DefaultAdmin");
            var verifierSection = config.GetSection("AdminAccounts:Verifier");

            var mainAdmin = admins.FirstOrDefault(a => a.Email == (defaultAdminSection["Email"] ?? "admin@legalmate.com"));
            if (mainAdmin != null)
            {
                profiles.Add(new AdminProfile
                {
                    Id = Guid.NewGuid(),
                    AdminId = mainAdmin.Id,
                    JobTitle = defaultAdminSection["JobTitle"] ?? "مدير النظام العام",
                    Department = defaultAdminSection["Department"] ?? "إدارة النظام",
                    FirstName = defaultAdminSection["FirstName"] ?? "سامي",
                    LastName = defaultAdminSection["LastName"] ?? "عبدالعزيز محمود",
                    NationalId = encryption.Encrypt(defaultAdminSection["NationalId"] ?? "27806224112345"),
                    DateOfBirth = DateTime.TryParse(defaultAdminSection["DateOfBirth"], out var dob1) ? dob1 : new DateTime(1978, 6, 22),
                    Nationality = defaultAdminSection["Nationality"] ?? "مصري",
                    GovernorateId = 1,
                    CityId = 106,
                    Address = defaultAdminSection["Address"] ?? "شارع قصر العيني، مبنى وزارة العدل، الدور الرابع",
                    JoinDate = DateTime.TryParse(defaultAdminSection["JoinDate"], out var jd1) ? jd1 : new DateTime(2022, 3, 1),
                    AlternativePhone = encryption.Encrypt(defaultAdminSection["AlternativePhone"] ?? "+20 2 2794 1234"),
                    TotalVerifiedLawyers = 15,
                    TotalRejectedLawyers = 3,
                    LastActiveAt = DateTime.UtcNow
                });
            }

            var verifierAdmin = admins.FirstOrDefault(a => a.Email == (verifierSection["Email"] ?? "verifier@legalmate.com"));
            if (verifierAdmin != null)
            {
                profiles.Add(new AdminProfile
                {
                    Id = Guid.NewGuid(),
                    AdminId = verifierAdmin.Id,
                    JobTitle = verifierSection["JobTitle"] ?? "مدقق محامين أول",
                    Department = verifierSection["Department"] ?? "قسم التوثيق والمراجعة",
                    FirstName = verifierSection["FirstName"] ?? "أحمد",
                    LastName = verifierSection["LastName"] ?? "رضا الشافعي",
                    NationalId = encryption.Encrypt(verifierSection["NationalId"] ?? "28503151234567"),
                    DateOfBirth = DateTime.TryParse(verifierSection["DateOfBirth"], out var dob2) ? dob2 : new DateTime(1985, 3, 15),
                    Nationality = verifierSection["Nationality"] ?? "مصري",
                    GovernorateId = 1,
                    CityId = 101,
                    Address = verifierSection["Address"] ?? "شارع عباس العقاد، برج النيل، الدور السابع",
                    JoinDate = DateTime.TryParse(verifierSection["JoinDate"], out var jd2) ? jd2 : new DateTime(2023, 6, 15),
                    AlternativePhone = encryption.Encrypt(verifierSection["AlternativePhone"] ?? "+20 2 2794 5678"),
                    TotalVerifiedLawyers = 38,
                    TotalRejectedLawyers = 7,
                    LastActiveAt = DateTime.UtcNow
                });
            }

            return profiles;
        }
    }
}