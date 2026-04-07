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
        public class LawyerAvailability
    {
        public Guid Id { get; set; }
        public Guid LawyerId { get; set; }
        public DayOfWeek Day { get; set; }
        public string DayName { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; }
        
        public LawyerProfile Lawyer { get; set; } = null!;
    }
}