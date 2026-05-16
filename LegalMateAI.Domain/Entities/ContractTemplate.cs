using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    public class ContractTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ContractType Type { get; set; }
        public string? Description { get; set; }
        public string TemplateContent { get; set; } = string.Empty; // للنصوص فقط
        public string? TemplateFilePath { get; set; } // ✅ المسار لملف Word الأصلي
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}