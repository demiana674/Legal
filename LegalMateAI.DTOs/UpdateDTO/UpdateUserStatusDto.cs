using LegalMateAI.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class AdminUpdateUserStatusDto
    {
        [Required]
        public AccountStatus Status { get; set; }

        public string? Reason { get; set; }
    }
}