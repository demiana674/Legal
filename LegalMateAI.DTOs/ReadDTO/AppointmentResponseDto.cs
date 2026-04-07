using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class AppointmentResponseDto
    {
        public Guid Id { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty;
        public string AppointmentType { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string DateFormatted { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public AppointmentStatus Status { get; set; }
        
        // ✅ Computed Properties (محسوبة من Status)
        public string StatusName => Status switch
        {
            AppointmentStatus.Pending => "قيد الانتظار",
            AppointmentStatus.Confirmed => "مؤكد",
            AppointmentStatus.Rescheduled => "معاد جدولته",
            AppointmentStatus.Completed => "مكتمل",
            AppointmentStatus.Cancelled => "ملغي",
            AppointmentStatus.NoShow => "لم يحضر",
            _ => Status.ToString()
        };
        
        public string StatusColor => Status switch
        {
            AppointmentStatus.Pending => "#F5A623",
            AppointmentStatus.Confirmed => "#3DD68C",
            AppointmentStatus.Rescheduled => "#4E9FE8",
            AppointmentStatus.Completed => "#4CAF50",
            AppointmentStatus.Cancelled => "#F44336",
            AppointmentStatus.NoShow => "#9E9E9E",
            _ => "#9E9E9E"
        };
        
        public DateTime RequestedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public bool IsUrgent { get; set; }
        
        // ✅ معلومات المالك للتحقق من الصلاحية
        public Guid UserId { get; set; }
        public Guid LawyerId { get; set; }
        
        public UserBriefDto User { get; set; } = null!;
        public LawyerBriefDto Lawyer { get; set; } = null!;
        public List<RescheduleResponseDto> Reschedules { get; set; } = new();
    }
}