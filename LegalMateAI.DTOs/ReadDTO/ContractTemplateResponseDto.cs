using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ContractTemplateResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ContractType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string TemplateContent { get; set; } = string.Empty; // للنصوص فقط
        public string TemplateFilePath { get; set; } = string.Empty; // المسار لملف Word
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}