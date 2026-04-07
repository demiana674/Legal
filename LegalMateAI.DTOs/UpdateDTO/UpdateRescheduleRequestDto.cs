using LegalMateAI.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateRescheduleRequestDto
    {
        [Required]
        public RescheduleStatus Status { get; set; }
    }
}