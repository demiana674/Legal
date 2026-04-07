using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateAppointmentDto
    {
        [StringLength(100)]
        public string? AppointmentType { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Date { get; set; }

        [RegularExpression(@"^(0[0-9]|1[0-2]):[0-5][0-9] (AM|PM)$")]
        public string? Time { get; set; }

        [Range(15, 240)]
        public int? DurationMinutes { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        public string? Notes { get; set; }

        public bool? IsUrgent { get; set; }
    }
}