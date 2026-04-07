using LegalMateAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
        public class AppointmentReschedule
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public DateTime OldDate { get; set; }
        public string OldTime { get; set; } = string.Empty;
        public DateTime NewDate { get; set; }
        public string NewTime { get; set; } = string.Empty;
        public RescheduleInitiator InitiatedBy { get; set; }
        public RescheduleStatus Status { get; set; }
        public string? Reason { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        
        public Appointment Appointment { get; set; } = null!;
    }
}