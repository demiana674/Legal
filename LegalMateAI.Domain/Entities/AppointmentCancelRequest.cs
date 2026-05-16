using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    public class AppointmentCancelRequest
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public virtual Appointment? Appointment { get; set; }
        public CancelInitiator RequestedBy { get; set; }
        public string Reason { get; set; } = string.Empty;
        public CancelRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string? ResponseReason { get; set; }
    }
}