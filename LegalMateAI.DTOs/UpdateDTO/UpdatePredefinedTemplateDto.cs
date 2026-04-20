// LegalMateAI.DTOs/UpdateDTO/UpdatePredefinedTemplateDto.cs
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.UpdateDTO
{
    /// <summary>
    /// نموذج تحديث قالب عقد
    /// </summary>
    public class UpdatePredefinedTemplateDto
    {
        [StringLength(200, MinimumLength = 3)]
        public string? Name { get; set; }
        
        [StringLength(200)]
        public string? NameEn { get; set; }
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        public bool? IsActive { get; set; }
        
        public bool? IsFeatured { get; set; }
        
        [MinLength(1)]
        public List<string>? RequiredFields { get; set; }
        
        [StringLength(500)]
        public string? SearchKeywords { get; set; }
    }
}