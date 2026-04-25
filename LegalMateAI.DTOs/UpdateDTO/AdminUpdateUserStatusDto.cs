using System.ComponentModel.DataAnnotations;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class AdminUpdateUserStatusDto
    {
        [Required(ErrorMessage = "الحالة مطلوبة")]
        public AccountStatus Status { get; set; }

        public string? Reason { get; set; }
    }
}