using LegalMateAI.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateContractTemplateDto
    {
        [Required(ErrorMessage = "اسم النموذج مطلوب")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "نوع العقد مطلوب")]
        public ContractType Type { get; set; }

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "محتوى النموذج مطلوب")]
        public string TemplateContent { get; set; } = string.Empty;

        public string[]? Placeholders { get; set; }
    }
}