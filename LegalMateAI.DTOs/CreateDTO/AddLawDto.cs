// LegalMateAI.DTOs/CreateDTO/AddLawDto.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class AddLawDto
    {
        [Required(ErrorMessage = "ملف PDF مطلوب")]
        public IFormFile PdfFile { get; set; } = null!;
        
        [Required(ErrorMessage = "اسم القانون مطلوب")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "تصنيف القانون مطلوب")]
        public LawCategory Category { get; set; }
        
        [StringLength(50)]
        public string? LawNumber { get; set; }
        
        [Range(1800, 2100)]
        public int? Year { get; set; }
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Url(ErrorMessage = "رابط المصدر غير صحيح")]
        public string? SourceUrl { get; set; }
        
        [StringLength(500)]
        public string? SearchKeywords { get; set; }
    }
}