using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateRescheduleRequestDto
    {
        [Required(ErrorMessage = "معرف الموعد مطلوب")]
        public Guid AppointmentId { get; set; }

        [Required(ErrorMessage = "التاريخ الجديد مطلوب")]
        [DataType(DataType.Date)]
        public DateTime NewDate { get; set; }

        [Required(ErrorMessage = "الوقت الجديد مطلوب")]
        [RegularExpression(@"^(0[0-9]|1[0-2]):[0-5][0-9] (AM|PM)$")]
        public string NewTime { get; set; } = string.Empty;

        public string? Reason { get; set; }
    }
}