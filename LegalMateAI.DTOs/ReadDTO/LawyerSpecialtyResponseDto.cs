// LegalMateAI.DTOs/ReadDTO/LawyerSpecialtyResponseDto.cs
using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawyerSpecialtyResponseDto
    {
        public int Id { get; set; }  // ✅ تغيير من Guid إلى int
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}