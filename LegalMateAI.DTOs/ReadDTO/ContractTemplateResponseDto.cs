using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    // 8. Contract Template Response
    public class ContractTemplateResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ContractType Type { get; set; }
        public string TypeName => Type.ToString();
        public string Description { get; set; } = string.Empty;
        public string TemplateContent { get; set; } = string.Empty;
        public string[]? Placeholders { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }      // ✅ أضف هذا
        public DateTime? UpdatedAt { get; set; } 
    }
}

