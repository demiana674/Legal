// LegalMateAI.DTOs/CreateDTO/CreateCaseNoteDto.cs
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateCaseNoteDto
    {
        [Required(ErrorMessage = "معرف القضية مطلوب")]
        public Guid CaseId { get; set; }
        
        [Required(ErrorMessage = "نص الملاحظة مطلوب")]
        public string Content { get; set; } = string.Empty;
        
        public bool IsPrivate { get; set; } = false;
    }
}