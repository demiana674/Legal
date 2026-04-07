// LegalMateAI.DTOs/CreateDTO/CreateDocumentDto.cs
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateDocumentDto
    {
        [Required(ErrorMessage = "الملف مطلوب")]
        public IFormFile File { get; set; } = null!;

        public string? Description { get; set; }
    }
}