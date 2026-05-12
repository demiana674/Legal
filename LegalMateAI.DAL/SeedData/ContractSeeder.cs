// ContractSeeder.cs
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LegalMateAI.DAL.SeedData
{
    public static class ContractSeeder
    {
        public static async Task SeedContractsFromJsonAsync(LegalMateDbContext context, ILogger logger, string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                logger.LogWarning($"❌ JSON file not found: {jsonFilePath}");
                return;
            }

            var json = await File.ReadAllTextAsync(jsonFilePath);
            var contractData = JsonSerializer.Deserialize<List<ContractJsonModel>>(json);

            if (contractData == null || !contractData.Any())
                return;

            var contracts = new List<Law>();

            foreach (var c in contractData)
            {
                if (string.IsNullOrWhiteSpace(c.name) || c.name.Length < 3) continue;

                // تحديد التصنيف حسب نوع العقد
                var category = c.type?.ToLower() switch
                {
                    "sale" => LawCategory.RealEstate,
                    "rental" => LawCategory.RealEstate,
                    "employment" => LawCategory.Labor,
                    "service" => LawCategory.Commercial,
                    _ => LawCategory.Commercial
                };

                contracts.Add(new Law
                {
                    Id = Guid.NewGuid(),
                    Name = c.name.Length > 200 ? c.name[..200] : c.name,
                    Category = category,
                    Description = c.description?.Length > 2000 ? c.description[..2000] : c.description,
                    SourceUrl = c.sourceUrl,
                    PdfFileUrl = c.pdfUrl,
                    SearchKeywords = c.searchKeywords,
                    IsActive = true,
                    IsApproved = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (contracts.Any())
            {
                await context.Laws.AddRangeAsync(contracts);
                await context.SaveChangesAsync();
                logger.LogInformation($"✅ Seeded {contracts.Count} contract templates from JSON");
            }
        }

        private class ContractJsonModel
        {
            public string? name { get; set; }
            public string? type { get; set; }
            public string? description { get; set; }
            public string? sourceUrl { get; set; }
            public string? pdfUrl { get; set; }
            public string? searchKeywords { get; set; }
        }
    }
}