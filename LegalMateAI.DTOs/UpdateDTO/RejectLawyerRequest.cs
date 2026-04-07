// LegalMateAI.DTOs/UpdateDTO/RejectLawyerRequest.cs
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class RejectLawyerRequest
    {
        [Required(ErrorMessage = "سبب الرفض مطلوب")]
        public string Reason { get; set; } = string.Empty;
    }
}