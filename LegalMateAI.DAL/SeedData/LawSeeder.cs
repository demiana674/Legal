using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LegalMateAI.DAL.SeedData
{
    public static class LawSeeder
    {
        public static async Task SeedEgyptianLawsAsync(LegalMateDbContext context, ILogger logger)
        {
            logger.LogInformation("🌱 Seeding basic Egyptian Laws...");

            var existingCount = await context.Laws.CountAsync();
            if (existingCount > 0)
            {
                logger.LogInformation($"⏭️ Laws already exist ({existingCount} records), skipping...");
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
            // التحقق من وجود بيانات فعلية
            var existingCount = await context.Laws.CountAsync();
            if (existingCount > 0)
            {
                logger.LogInformation($"⏭️ Laws already exist ({existingCount} records), skipping...");
                return;
            }

            if (!File.Exists(jsonFilePath))
            {
                logger.LogWarning($"❌ JSON file not found: {jsonFilePath}");
                await SeedEgyptianLawsAsync(context, logger);
                return;
            }

            var json = await File.ReadAllTextAsync(jsonFilePath);
            
            // استخدام خيارات إضافية لتفادي مشاكل التحويل
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
            
            var lawData = JsonSerializer.Deserialize<List<ManshuratLaw>>(json, options);

            if (lawData == null || !lawData.Any())
            {
                logger.LogWarning("⚠️ JSON file is empty or invalid");
                await SeedEgyptianLawsAsync(context, logger);
                return;
            }

            logger.LogInformation($"📄 Found {lawData.Count} laws in JSON file");

            var laws = new List<Law>();
            int skipped = 0;
            int added = 0;

            foreach (var d in lawData)
            {
                if (string.IsNullOrWhiteSpace(d.name) || d.name.Length < 3)
                {
                    skipped++;
                    continue;
                }

                // بناء الوصف من الحقول المتاحة
                string description = BuildDescription(d);

                // استخراج رقم القانون
                string lawNumber = !string.IsNullOrEmpty(d.lawNumber) ? d.lawNumber : d.description?.documentNumber;
                if (string.IsNullOrEmpty(lawNumber))
                {
                    var match = Regex.Match(d.name ?? "", @"رقم\s*(\d+)");
                    if (match.Success)
                        lawNumber = match.Groups[1].Value;
                }

                // استخراج السنة
                int year = d.year ?? 0;
                if (year == 0 && !string.IsNullOrEmpty(d.description?.dateIssued))
                {
                    var yearMatch = Regex.Match(d.description.dateIssued, @"\d{4}");
                    if (yearMatch.Success)
                        year = int.Parse(yearMatch.Value);
                }

                laws.Add(new Law
                {
                    Id = Guid.NewGuid(),
                    Name = d.name.Length > 200 ? d.name[..200] : d.name,
                    LawNumber = lawNumber ?? "",
                    Year = year,
                    Category = ParseCategory(d.category),
                    Description = description?.Length > 2000 ? description[..2000] : description ?? "",
                    SourceUrl = d.sourceUrl,
                    PdfFileUrl = d.pdfUrl,
                    SearchKeywords = d.searchKeywords,
                    IsActive = true,
                    IsApproved = true,
                    CreatedAt = DateTime.UtcNow
                });
                added++;
            }

            logger.LogInformation($"📝 Preparing to insert {added} laws (skipped {skipped} invalid)");

            if (laws.Any())
            {
                const int batchSize = 500;
                for (int i = 0; i < laws.Count; i += batchSize)
                {
                    var batch = laws.Skip(i).Take(batchSize);
                    await context.Laws.AddRangeAsync(batch);
                    await context.SaveChangesAsync();
                    logger.LogInformation($"   ✅ Inserted {Math.Min(i + batchSize, laws.Count)}/{laws.Count} laws");
                }
            }

            logger.LogInformation($"✅ Successfully seeded {added} laws from JSON!");
        }

        private static string BuildDescription(ManshuratLaw d)
        {
            var parts = new List<string>();

            if (d.description == null)
                return d.name ?? "";

            if (!string.IsNullOrWhiteSpace(d.description.title))
                parts.Add($"العنوان: {d.description.title}");

            if (!string.IsNullOrWhiteSpace(d.description.docType))
                parts.Add($"نوع الوثيقة: {d.description.docType}");

            if (!string.IsNullOrWhiteSpace(d.description.sector))
                parts.Add($"القطاع: {d.description.sector}");

            if (!string.IsNullOrWhiteSpace(d.description.issuer))
                parts.Add($"جهة الإصدار: {d.description.issuer}");

            if (!string.IsNullOrWhiteSpace(d.description.issuerRole)
                && d.description.issuerRole.ToLower() != "other")
            {
                parts.Add($"الدور: {d.description.issuerRole}");
            }

            if (!string.IsNullOrWhiteSpace(d.description.documentNumber))
                parts.Add($"رقم الوثيقة: {d.description.documentNumber}");

            if (!string.IsNullOrWhiteSpace(d.description.dateIssued))
                parts.Add($"تاريخ الإصدار: {d.description.dateIssued}");

            if (!string.IsNullOrWhiteSpace(d.description.datePublished))
                parts.Add($"تاريخ النشر: {d.description.datePublished}");

            if (!string.IsNullOrWhiteSpace(d.description.dateEffective))
                parts.Add($"تاريخ العمل به: {d.description.dateEffective}");

            if (!string.IsNullOrWhiteSpace(d.description.summary))
            {
                var summary = d.description.summary;

                // حذف Facebook / Twitter
                summary = summary.Replace("Facebook", "");
                summary = summary.Replace("Twitter", "");

                // حذف الرموز الغريبة
                summary = summary.Replace("::::", "");
                summary = summary.Replace("›", " - ");

                // تنظيف المسافات
                summary = Regex.Replace(summary, @"\s+", " ").Trim();

                parts.Add($"الملخص: {summary}");
            }

            if (parts.Count == 0)
                return d.name ?? "";

            return string.Join(" | ", parts);
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
            public DescriptionData? description { get; set; }
            public string? sourceUrl { get; set; }
            public string? pdfUrl { get; set; }
            public string? searchKeywords { get; set; }
        }

        public class DescriptionData
        {
            public string? title { get; set; }
            public string? docType { get; set; }
            public string? sector { get; set; }
            public string? issuer { get; set; }
            public string? issuerRole { get; set; }
            public string? documentNumber { get; set; }
            public string? dateIssued { get; set; }
            public string? datePublished { get; set; }
            public string? dateEffective { get; set; }
            public string? summary { get; set; }
        }
    }
}