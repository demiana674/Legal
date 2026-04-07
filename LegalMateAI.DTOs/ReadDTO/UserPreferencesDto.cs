using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.ReadDTO
{
    // 5. التفضيلات
    public class UserPreferencesDto
    {
        // إشعارات
        public bool EmailNotifications { get; set; }
        public bool SmsNotifications { get; set; }
        public bool PushNotifications { get; set; }
        
        // تذكيرات
        public bool AppointmentReminders { get; set; }
        public int ReminderBeforeHours { get; set; }
        
        // خصوصية
        public bool ShowEmail { get; set; }
        public bool ShowPhone { get; set; }
        public bool ShowBirthDate { get; set; }
        
    
    }
}