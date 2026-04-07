// LegalMateAI.DTOs/ReadDTO/AdminLogDto.cs
using System;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class AdminLogDto
    {
        public Guid Id { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public AdminLogAction Action { get; set; }
        
        public string ActionName => Action switch
        {
            AdminLogAction.Verify => "موافقة على محامي",
            AdminLogAction.Reject => "رفض محامي",
            AdminLogAction.Login => "تسجيل دخول",
            AdminLogAction.UpdateProfile => "تحديث الملف الشخصي",
            AdminLogAction.Suspend => "تعليق",
            AdminLogAction.Activate => "تنشيط",
            _ => Action.ToString()
        };
        
        public string ActionIcon => Action switch
        {
            AdminLogAction.Verify => "✅",
            AdminLogAction.Reject => "❌",
            AdminLogAction.Login => "🔐",
            AdminLogAction.UpdateProfile => "✏️",
            AdminLogAction.Suspend => "⏸️",
            AdminLogAction.Activate => "▶️",
            _ => "📌"
        };
        
        public string TargetType { get; set; } = string.Empty;
        
        public string TargetTypeAr => TargetType switch
        {
            "Lawyer" => "محامي",
            "User" => "مستخدم",
            "Admin" => "أدمن",
            "System" => "نظام",
            _ => "نظام"
        };
        
        public Guid TargetId { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimestampFormatted => Timestamp.ToString("dd MMM yyyy HH:mm");
        public string TimeAgo => GetRelativeTime(Timestamp);

        private string GetRelativeTime(DateTime dateTime)
        {
            var diff = DateTime.UtcNow - dateTime;
            if (diff.TotalMinutes < 1) return "الآن";
            if (diff.TotalMinutes < 60) return $"منذ {diff.Minutes} دقيقة";
            if (diff.TotalHours < 24) return $"منذ {diff.Hours} ساعة";
            if (diff.TotalDays < 30) return $"منذ {diff.Days} يوم";
            return dateTime.ToString("dd MMM yyyy");
        }
    }
}