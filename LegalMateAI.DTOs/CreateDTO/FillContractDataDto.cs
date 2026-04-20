// LegalMateAI.DTOs/CreateDTO/FillContractDataDto.cs
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    /// <summary>
    /// نموذج تعبئة بيانات العقد
    /// </summary>
    public class FillContractDataDto
    {
        [Required(ErrorMessage = "معرف القالب مطلوب")]
        public Guid TemplateId { get; set; }
        
        [Required(ErrorMessage = "البيانات المطلوبة غير مكتملة")]
        public Dictionary<string, string> FilledData { get; set; } = new();
        
        public Guid? LawyerId { get; set; }
        
        [StringLength(200)]
        public string? CustomTitle { get; set; }
        
        [RegularExpression("^(pdf|docx)$", ErrorMessage = "الصيغة يجب أن تكون pdf أو docx")]
        public string OutputFormat { get; set; } = "pdf";
    }
}