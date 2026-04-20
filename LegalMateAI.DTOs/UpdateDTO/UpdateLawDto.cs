// LegalMateAI.DTOs/UpdateDTO/UpdateLawDto.cs
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.UpdateDTO
{
    /// <summary>
    /// نموذج تحديث قانون
    /// </summary>
    public class UpdateLawDto
    {
        /// <summary>
        /// اسم القانون
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// تصنيف القانون
        /// </summary>
        public LawCategory? Category { get; set; }
        
        /// <summary>
        /// رقم القانون
        /// </summary>
        public string? LawNumber { get; set; }
        
        /// <summary>
        /// سنة الإصدار
        /// </summary>
        public int? Year { get; set; }
        
        /// <summary>
        /// وصف القانون
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// رابط المصدر
        /// </summary>
        public string? SourceUrl { get; set; }
        
        /// <summary>
        /// كلمات مفتاحية للبحث
        /// </summary>
        public string? SearchKeywords { get; set; }
        
        /// <summary>
        /// هل القانون نشط؟
        /// </summary>
        public bool? IsActive { get; set; }
    }
}