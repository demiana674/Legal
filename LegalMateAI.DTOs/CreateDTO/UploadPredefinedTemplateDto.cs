// LegalMateAI.DTOs/CreateDTO/UploadPredefinedTemplateDto.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.CreateDTO
{
    /// <summary>
    /// نموذج رفع قالب عقد جديد
    /// </summary>
    public class UploadPredefinedTemplateDto
    {
        [Required(ErrorMessage = "الملف مطلوب")]
        public IFormFile File { get; set; } = null!;
        
        [Required(ErrorMessage = "اسم القالب مطلوب")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "اسم القالب يجب أن يكون بين 3 و 200 حرف")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? NameEn { get; set; }
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "نوع العقد مطلوب")]
        public ContractType ContractType { get; set; }
        
        [Required(ErrorMessage = "الحقول المطلوبة مطلوبة")]
        [MinLength(1, ErrorMessage = "يجب تحديد حقل واحد على الأقل")]
        public List<string> RequiredFields { get; set; } = new();
        
        [StringLength(500)]
        public string? SearchKeywords { get; set; }
        
        public bool IsFeatured { get; set; }
        
        public IFormFile? Thumbnail { get; set; }
    }
}