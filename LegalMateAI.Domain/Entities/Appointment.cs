using System;
using System.Collections.Generic;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    public class Appointment
    {
        public Guid Id { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty;
        public Guid UserID { get; set; }
        public Guid LawyerId { get; set; }
        
        /// <summary>
        /// معرف الفرع (اختياري)
        /// </summary>
        public Guid? BranchId { get; set; }
        public LawyerBranch? Branch { get; set; }
        
        public string AppointmentType { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Time { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public AppointmentStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public bool IsUrgent { get; set; }
        
        // Navigation properties
        // public User User { get; set; } = null!;
        public LawyerProfile Lawyer { get; set; } = null!;
        public ICollection<AppointmentReschedule> Reschedules { get; set; } = new List<AppointmentReschedule>();
    }
}