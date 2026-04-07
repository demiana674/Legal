// LegalMateAI.DTOs/ReadDTO/SpecializationDto.cs
using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class SpecializationDto
    {
        public int Id { get; set; }  // ← غير من Guid إلى int
        public string Name { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int CasesCount { get; set; }
    }
}