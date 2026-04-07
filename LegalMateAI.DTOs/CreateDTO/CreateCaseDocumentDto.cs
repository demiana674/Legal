// LegalMateAI.DTOs/CreateDTO/CreateCaseDocumentDto.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateCaseDocumentDto
    {
        [Required(ErrorMessage = "معرف القضية مطلوب")]
        public Guid CaseId { get; set; }
        
        [Required(ErrorMessage = "الملف مطلوب")]
        public IFormFile File { get; set; } = null!;
        
        public string? Description { get; set; }
    }
}