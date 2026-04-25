using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class ChatRequestDto
    {
        [Required(ErrorMessage = "الرسالة مطلوبة")]
        [StringLength(2000, MinimumLength = 1)]
        public string Message { get; set; } = string.Empty;
        
        public string? SessionId { get; set; }
        public bool ClearHistory { get; set; }
    }
}