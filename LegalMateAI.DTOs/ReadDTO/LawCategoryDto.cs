// LegalMateAI.DTOs/ReadDTO/LawCategoryDto.cs
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    /// <summary>
    /// تصنيفات القوانين مع عدد القوانين في كل تصنيف
    /// </summary>
    public class LawCategoryDto
    {
        public LawCategory Category { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameEn => Category.ToString();
        public int Count { get; set; }
        
        public string Icon => Category switch
        {
            LawCategory.Constitutional => "🏛️",
            LawCategory.Civil => "⚖️",
            LawCategory.Commercial => "💼",
            LawCategory.Criminal => "🔒",
            LawCategory.Family => "👨‍👩‍👧‍👦",
            LawCategory.Labor => "👷",
            LawCategory.Tax => "💰",
            LawCategory.Administrative => "📋",
            LawCategory.RealEstate => "🏠",
            LawCategory.Investment => "📈",
            _ => "📄"
        };
        
        public string Color => Category switch
        {
            LawCategory.Constitutional => "#C8A84B",
            LawCategory.Civil => "#3DD68C",
            LawCategory.Commercial => "#4E9FE8",
            LawCategory.Criminal => "#F2605A",
            LawCategory.Family => "#9B6FF5",
            LawCategory.Labor => "#F5A623",
            LawCategory.Tax => "#E84E9E",
            _ => "#9E9E9E"
        };
    }
}