using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
namespace LegalMateAI.DTOs.UpdateDTO
{
    // 4. تحديث التفضيلات
    public class UpdateUserPreferencesDto
    {
        public bool? EmailNotifications { get; set; }
        public bool? SmsNotifications { get; set; }
        public bool? PushNotifications { get; set; }
        public bool? AppointmentReminders { get; set; }
        public int? ReminderBeforeHours { get; set; }
        public bool? ShowEmail { get; set; }
        public bool? ShowPhone { get; set; }
        public bool? ShowBirthDate { get; set; }
        public string? Language { get; set; }
    }
}