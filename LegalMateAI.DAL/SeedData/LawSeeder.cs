using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LegalMateAI.DAL.SeedData
{
    public static class LawSeeder
    {
        public static async Task SeedEgyptianLawsAsync(LegalMateDbContext context, ILogger logger)
        {
            logger.LogInformation("🌱 Seeding basic Egyptian Laws...");

            if (await context.Laws.AnyAsync())
            {
                logger.LogInformation("⏭️ Laws already exist, skipping...");
                return;
            }

            var laws = new List<Law>
            {
                new() { Id = Guid.NewGuid(), Name = "القانون المدني المصري", LawNumber = "131", Year = 1948, Category = LawCategory.Civil, Description = "القانون المدني المصري رقم 131 لسنة 1948", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون العقوبات المصري", LawNumber = "58", Year = 1937, Category = LawCategory.Criminal, Description = "قانون العقوبات رقم 58 لسنة 1937", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون الإجراءات الجنائية", LawNumber = "150", Year = 1950, Category = LawCategory.Criminal, Description = "قانون الإجراءات الجنائية", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون التجارة المصري", LawNumber = "17", Year = 1999, Category = LawCategory.Commercial, Description = "قانون التجارة المصري", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون العمل المصري", LawNumber = "12", Year = 2003, Category = LawCategory.Labor, Description = "قانون العمل المصري", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون الأحوال الشخصية", LawNumber = "25", Year = 1929, Category = LawCategory.Family, Description = "قانون الأحوال الشخصية", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون الاستثمار", LawNumber = "72", Year = 2017, Category = LawCategory.Investment, Description = "قانون الاستثمار", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون حماية المستهلك", LawNumber = "181", Year = 2018, Category = LawCategory.Commercial, Description = "قانون حماية المستهلك", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون الضريبة على الدخل", LawNumber = "91", Year = 2005, Category = LawCategory.Tax, Description = "قانون الضريبة على الدخل", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون مجلس الدولة", LawNumber = "47", Year = 1972, Category = LawCategory.Administrative, Description = "قانون مجلس الدولة", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "الدستور المصري", Year = 2014, Category = LawCategory.Constitutional, Description = "دستور جمهورية مصر العربية", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون المرافعات المدنية", LawNumber = "13", Year = 1968, Category = LawCategory.Civil, Description = "قانون المرافعات", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون الإثبات", LawNumber = "25", Year = 1968, Category = LawCategory.Civil, Description = "قانون الإثبات", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون الطفل", LawNumber = "12", Year = 1996, Category = LawCategory.Family, Description = "قانون الطفل المصري", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون الجمارك", LawNumber = "207", Year = 2020, Category = LawCategory.Tax, Description = "قانون الجمارك", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون البناء الموحد", LawNumber = "119", Year = 2008, Category = LawCategory.RealEstate, Description = "قانون البناء", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون إيجار الأماكن", LawNumber = "4", Year = 1996, Category = LawCategory.RealEstate, Description = "قانون إيجار الأماكن", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون التعليم", LawNumber = "139", Year = 1981, Category = LawCategory.Educational, Description = "قانون التعليم", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون الجامعات", LawNumber = "49", Year = 1972, Category = LawCategory.Educational, Description = "قانون تنظيم الجامعات", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "قانون المحاماة", LawNumber = "17", Year = 1983, Category = LawCategory.Administrative, Description = "قانون المحاماة", IsActive = true, IsApproved = true, CreatedAt = DateTime.UtcNow },
            };

            await context.Laws.AddRangeAsync(laws);
            await context.SaveChangesAsync();
            logger.LogInformation($"✅ Seeded {laws.Count} basic laws!");
        }

        public static async Task SeedFromJsonFileAsync(LegalMateDbContext context, ILogger logger, string jsonFilePath)
        {
            if (await context.Laws.AnyAsync())
            {
                logger.LogInformation("⏭️ Laws already exist, skipping...");
                return;
            }

            if (!File.Exists(jsonFilePath))
            {
                logger.LogWarning($"❌ JSON file not found: {jsonFilePath}");
                await SeedEgyptianLawsAsync(context, logger);
                return;
            }

            var json = await File.ReadAllTextAsync(jsonFilePath);
            var lawData = JsonSerializer.Deserialize<List<ManshuratLaw>>(json);

            if (lawData == null || !lawData.Any())
            {
                await SeedEgyptianLawsAsync(context, logger);
                return;
            }

            var laws = new List<Law>();
            foreach (var d in lawData)
            {
                if (string.IsNullOrWhiteSpace(d.name) || d.name.Length < 3) continue;

                laws.Add(new Law
                {
                    Id = Guid.NewGuid(),
                    Name = d.name.Length > 200 ? d.name[..200] : d.name,
                    LawNumber = d.lawNumber,
                    Year = d.year,
                    Category = ParseCategory(d.category),
                    Description = d.description != null && d.description.Length > 2000 ? d.description[..2000] : d.description,
                    SourceUrl = d.sourceUrl,
                    PdfFileUrl = d.pdfUrl,
                    SearchKeywords = d.searchKeywords,
                    IsActive = true,
                    IsApproved = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (laws.Any())
            {
                await context.Laws.AddRangeAsync(laws);
                await context.SaveChangesAsync();
            }

            logger.LogInformation($"✅ Seeded {laws.Count} laws from JSON");
        }

        private static LawCategory ParseCategory(int? category)
        {
            return category switch
            {
                1 => LawCategory.Constitutional,
                2 => LawCategory.Civil,
                3 => LawCategory.Criminal,
                4 => LawCategory.Commercial,
                5 => LawCategory.Labor,
                6 => LawCategory.Tax,
                7 => LawCategory.Family,
                8 => LawCategory.Procedure,
                9 => LawCategory.RealEstate,
                10 => LawCategory.Financial,
                11 => LawCategory.Investment,
                12 => LawCategory.Social,
                13 => LawCategory.Educational,
                14 => LawCategory.Economic,
                15 => LawCategory.Maritime,
                16 => LawCategory.Administrative,
                17 => LawCategory.International,
                _ => LawCategory.Other
            };
        }

        public class ManshuratLaw
        {
            public string? name { get; set; }
            public string? lawNumber { get; set; }
            public int? year { get; set; }
            public int? category { get; set; }
            public string? description { get; set; }
            public string? sourceUrl { get; set; }
            public string? pdfUrl { get; set; }
            public string? searchKeywords { get; set; }
        }
    }
}