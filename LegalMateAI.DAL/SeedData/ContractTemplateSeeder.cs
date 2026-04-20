// LegalMateAI.DAL/SeedData/ContractTemplateSeeder.cs
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.DAL.SeedData
{
    public static class ContractTemplateSeeder
    {
        public static async Task SeedTemplatesAsync(LegalMateDbContext context, string webRootPath, ILogger logger)
        {
            logger.LogInformation("🌱 Seeding contract templates from files...");

            var templatesFolder = Path.Combine(webRootPath, "uploads", "contracts", "templates");
            
            logger.LogInformation($"📁 Looking in folder: {templatesFolder}");
            
            if (!Directory.Exists(templatesFolder))
            {
                logger.LogWarning($"❌ Templates folder not found: {templatesFolder}");
                return;
            }

            var templateFiles = Directory.GetFiles(templatesFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".docx") || f.EndsWith(".doc"))
                .ToList();

            logger.LogInformation($"📄 Found {templateFiles.Count} template files");

            foreach (var filePath in templateFiles)
            {
                var fileName = Path.GetFileName(filePath);
                var relativePath = filePath.Replace(webRootPath, "").Replace("\\", "/");
                
                var existingTemplate = await context.ContractTemplates
                    .FirstOrDefaultAsync(t => t.TemplateContent == relativePath);

                if (existingTemplate == null)
                {
                    var contractType = DetermineContractType(filePath, fileName);
                    
                    var template = new ContractTemplate
                    {
                        Id = Guid.NewGuid(),
                        Name = Path.GetFileNameWithoutExtension(fileName),
                        Type = contractType,
                        Description = GetDescriptionFromFileName(fileName),
                        TemplateContent = relativePath,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.ContractTemplates.Add(template);
                    logger.LogInformation($"✅ Added: {template.Name}");
                }
            }

            await context.SaveChangesAsync();
            logger.LogInformation($"🎉 Template seeding completed! Total: {await context.ContractTemplates.CountAsync()}");
        }

        private static ContractType DetermineContractType(string filePath, string fileName)
        {
            var nameLower = fileName.ToLower();

            if (nameLower.Contains("ايجار") || nameLower.Contains("إيجار") || nameLower.Contains("تأجير") || nameLower.Contains("استئجار"))
                return ContractType.Rental;
            
            if (nameLower.Contains("عمل") || nameLower.Contains("توظيف"))
                return ContractType.Employment;
            
            if (nameLower.Contains("بيع") || nameLower.Contains("شراء"))
                return ContractType.Sale;
            
            if (nameLower.Contains("خدمات") || nameLower.Contains("استشار"))
                return ContractType.Service;
            
            if (nameLower.Contains("شراكة") || nameLower.Contains("شركة") || nameLower.Contains("تأسيس"))
                return ContractType.Partnership;
            
            if (nameLower.Contains("وكالة") || nameLower.Contains("توكيل") || nameLower.Contains("وكيل"))
                return ContractType.PowerOfAttorney;
            
            if (nameLower.Contains("صلح") || nameLower.Contains("تسوية"))
                return ContractType.Settlement;

            if (nameLower.Contains("مقاولة") || nameLower.Contains("بناء") || nameLower.Contains("تشييد"))
                return ContractType.Other;

            return ContractType.Other;
        }

        private static string GetDescriptionFromFileName(string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            
            if (name.Contains("ايجار") || name.Contains("إيجار"))
                return "عقد إيجار رسمي";
            if (name.Contains("بيع"))
                return "عقد بيع رسمي";
            if (name.Contains("عمل"))
                return "عقد عمل رسمي";
            if (name.Contains("شركة") || name.Contains("تأسيس"))
                return "عقد تأسيس شركة";
            if (name.Contains("وكالة") || name.Contains("توكيل"))
                return "وكالة قانونية";
            if (name.Contains("صلح"))
                return "عقد صلح وتسوية";
            if (name.Contains("وصية"))
                return "صيغة وصية";
            if (name.Contains("قرض"))
                return "عقد قرض";
            if (name.Contains("رهن"))
                return "عقد رهن";
            if (name.Contains("كفالة"))
                return "عقد كفالة";
            if (name.Contains("هبة"))
                return "عقد هبة";
            if (name.Contains("مقايضة"))
                return "عقد مقايضة";
            if (name.Contains("قسمة"))
                return "عقد قسمة";
            if (name.Contains("اقرار") || name.Contains("إقرار"))
                return "إقرار قانوني";
            
            return "قالب عقد قانوني";
        }
    }
}