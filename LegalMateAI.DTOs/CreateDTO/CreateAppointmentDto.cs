using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateAppointmentDto
    {
        [Required(ErrorMessage = "معرف المحامي مطلوب")]
        public Guid LawyerId { get; set; }

        [Required(ErrorMessage = "نوع الموعد مطلوب")]
        [StringLength(100)]
        public string AppointmentType { get; set; } = string.Empty;

        [Required(ErrorMessage = "التاريخ مطلوب")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "الوقت مطلوب")]
        [RegularExpression(@"^(0[0-9]|1[0-2]):[0-5][0-9] (AM|PM)$", 
            ErrorMessage = "صيغة الوقت غير صحيحة (مثال: 10:00 AM)")]
        public string Time { get; set; } = string.Empty;

        [Range(15, 240, ErrorMessage = "المدة يجب أن تكون بين 15 و 240 دقيقة")]
        public int DurationMinutes { get; set; } = 60;

        [Required(ErrorMessage = "المكان مطلوب")]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public bool IsUrgent { get; set; }
    }
}