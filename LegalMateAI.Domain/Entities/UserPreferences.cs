 using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegalMateAI.Domain.Enums;
using LegalMateAI.Domain.Entities;
 
namespace LegalMateAI.Domain.Entities
{
 // تفضيلات المستخدم
    public class UserPreferences
    {
        public Guid Id { get; set; }
        public Guid UserProfileId { get; set; }
        public UserProfile UserProfile { get; set; } = null!;
        
        // إشعارات
        public bool EmailNotifications { get; set; } = true;
        public bool SmsNotifications { get; set; } = false;
        public bool PushNotifications { get; set; } = true;
        
        // تذكيرات
        public bool AppointmentReminders { get; set; } = true;
        public int ReminderBeforeHours { get; set; } = 24;
        
        // خصوصية
        public bool ShowEmail { get; set; } = false;
        public bool ShowPhone { get; set; } = false;
        public bool ShowBirthDate { get; set; } = false;
        
        // اللغة
        public string Language { get; set; } = "ar";
    }
}