using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class CancelRequestResponseDto
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public CancelInitiator RequestedBy { get; set; }
        public string Reason { get; set; } = string.Empty;
        public CancelRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string? ResponseReason { get; set; }
        
        // معلومات إضافية للموعد
        public DateTime AppointmentDate { get; set; }
        public string AppointmentTime { get; set; } = string.Empty;
        public string LawyerName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}