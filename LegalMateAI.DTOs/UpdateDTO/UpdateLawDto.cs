// LegalMateAI.DTOs/UpdateDTO/UpdateLawDto.cs
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateLawDto
    {
        public string? Name { get; set; }
        public string? LawNumber { get; set; }
        public int? Year { get; set; }
        public LawCategory? Category { get; set; }
        public string? Description { get; set; }
        public string? SourceUrl { get; set; }
        public string? SearchKeywords { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsApproved { get; set; }
        
        /// <summary>
        /// ملف PDF جديد (اختياري - لاستبدال الملف القديم)
        /// </summary>
        public IFormFile? PdfFile { get; set; }
        
        /// <summary>
        /// رابط PDF خارجي جديد
        /// </summary>
        public string? PdfFileUrl { get; set; }
    }
}