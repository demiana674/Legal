// LegalMateAI.DAL/Seeders/LawyerSkillSeeder.cs
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.DAL.Seeders
{
    /// <summary>
    /// Seeder لإضافة المهارات الافتراضية للمحامين
    /// </summary>
    public static class LawyerSkillSeeder
    {
        /// <summary>
        /// قائمة المهارات الافتراضية
        /// </summary>
        public static List<LawyerSkill> GetDefaultSkills()
        {
            return new List<LawyerSkill>
            {
                // مهارات العقود والمحاماة التجارية
                new() { Name = "Contract Drafting", NameAr = "صياغة العقود", Category = "العقود", DisplayOrder = 1, Icon = "📄" },
                new() { Name = "Commercial Contracts", NameAr = "العقود التجارية", Category = "العقود", DisplayOrder = 2, Icon = "📊" },
                new() { Name = "Corporate Law", NameAr = "قانون الشركات", Category = "الشركات", DisplayOrder = 3, Icon = "🏢" },
                
                // مهارات المرافعة والتحكيم
                new() { Name = "Pleading", NameAr = "المرافعة", Category = "المرافعة", DisplayOrder = 4, Icon = "⚖️" },
                new() { Name = "International Arbitration", NameAr = "التحكيم الدولي", Category = "التحكيم", DisplayOrder = 5, Icon = "🌍" },
                new() { Name = "Domestic Arbitration", NameAr = "التحكيم المحلي", Category = "التحكيم", DisplayOrder = 6, Icon = "🏛️" },
                
                // مهارات قانونية متخصصة
                new() { Name = "Real Estate Law", NameAr = "القانون العقاري", Category = "العقارات", DisplayOrder = 7, Icon = "🏠" },
                new() { Name = "Property Disputes", NameAr = "النزاعات الملكية", Category = "النزاعات", DisplayOrder = 8, Icon = "⚔️" },
                new() { Name = "Labor Law", NameAr = "قانون العمل", Category = "العمل", DisplayOrder = 9, Icon = "👥" },
                
                // مهارات الاستشارات
                new() { Name = "Legal Consulting", NameAr = "الاستشارات القانونية", Category = "الاستشارات", DisplayOrder = 10, Icon = "💡" },
                new() { Name = "Tenders and Bids", NameAr = "المناقصات والعطاءات", Category = "المشتريات", DisplayOrder = 11, Icon = "📋" },
                
                // مهارات إضافية
                new() { Name = "Intellectual Property", NameAr = "الملكية الفكرية", Category = "الملكية الفكرية", DisplayOrder = 12, Icon = "©️" },
                new() { Name = "Banking Law", NameAr = "القانون المصرفي", Category = "البنوك", DisplayOrder = 13, Icon = "🏦" },
                new() { Name = "Insurance Law", NameAr = "قانون التأمين", Category = "التأمين", DisplayOrder = 14, Icon = "🛡️" },
                new() { Name = "Maritime Law", NameAr = "القانون البحري", Category = "البحري", DisplayOrder = 15, Icon = "⛵" },
                new() { Name = "Tax Law", NameAr = "القانون الضريبي", Category = "الضرائب", DisplayOrder = 16, Icon = "💰" },
                new() { Name = "Family Law", NameAr = "قانون الأسرة", Category = "الأحوال الشخصية", DisplayOrder = 17, Icon = "👨‍👩‍👧" },
                new() { Name = "Criminal Law", NameAr = "القانون الجنائي", Category = "الجنائي", DisplayOrder = 18, Icon = "🔒" },
                new() { Name = "Administrative Law", NameAr = "القانون الإداري", Category = "الإداري", DisplayOrder = 19, Icon = "📁" },
                new() { Name = "Constitutional Law", NameAr = "القانون الدستوري", Category = "الدستوري", DisplayOrder = 20, Icon = "📜" },
                new() { Name = "Human Rights", NameAr = "حقوق الإنسان", Category = "حقوق الإنسان", DisplayOrder = 21, Icon = "🤝" },
                new() { Name = "Environmental Law", NameAr = "القانون البيئي", Category = "البيئة", DisplayOrder = 22, Icon = "🌿" },
                new() { Name = "Telecom Law", NameAr = "قانون الاتصالات", Category = "الاتصالات", DisplayOrder = 23, Icon = "📡" },
                new() { Name = "Healthcare Law", NameAr = "قانون الرعاية الصحية", Category = "الصحة", DisplayOrder = 24, Icon = "🏥" },
                new() { Name = "Education Law", NameAr = "قانون التعليم", Category = "التعليم", DisplayOrder = 25, Icon = "🎓" },
                new() { Name = "Sports Law", NameAr = "القانون الرياضي", Category = "الرياضة", DisplayOrder = 26, Icon = "⚽" },
                new() { Name = "Media Law", NameAr = "قانون الإعلام", Category = "الإعلام", DisplayOrder = 27, Icon = "📺" },
                new() { Name = "Cyber Law", NameAr = "قانون الإنترنت", Category = "التقنية", DisplayOrder = 28, Icon = "💻" },
                new() { Name = "Data Protection", NameAr = "حماية البيانات", Category = "التقنية", DisplayOrder = 29, Icon = "🔐" },
                new() { Name = "Competition Law", NameAr = "قانون المنافسة", Category = "الاقتصاد", DisplayOrder = 30, Icon = "🏆" }
            };
        }

        /// <summary>
        /// تنفيذ Seeder المهارات
        /// </summary>
        public static async Task SeedAsync(LegalMateDbContext context, ILogger? logger = null)
        {
            try
            {
                // التحقق من وجود بيانات بالفعل
                if (await context.Set<LawyerSkill>().AnyAsync())
                {
                    logger?.LogInformation("✅ LawyerSkills already exist in database");
                    return;
                }

                logger?.LogInformation("🌱 Seeding LawyerSkills...");
                
                var skills = GetDefaultSkills();
                await context.Set<LawyerSkill>().AddRangeAsync(skills);
                await context.SaveChangesAsync();
                
                logger?.LogInformation($"✅ Successfully seeded {skills.Count} LawyerSkills");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "❌ Error seeding LawyerSkills");
                throw;
            }
        }
    }
}