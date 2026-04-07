using LegalMateAI.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateDocumentStatusDto
    {
        [Required]
        public DocumentStatus Status { get; set; }
    }
}