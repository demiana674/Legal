using LegalMateAI.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateAppointmentStatusDto
    {
        [Required(ErrorMessage = "الحالة مطلوبة")]
        public AppointmentStatus Status { get; set; }

        public string? CancellationReason { get; set; }
    }
}