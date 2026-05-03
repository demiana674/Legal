// LegalMateAI.DTOs/ReadDTO/AdminLogDto.cs
using System;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class AdminLogDto
    {
        public Guid Id { get; set; }

        // الشخص اللي نفذ العملية
        public string? Name { get; set; } = string.Empty;

        // بيانات الشخص اللي نفذ العملية
        public Guid ?ActorId { get; set; }
        public string? ActorRole { get; set; } = string.Empty;

        public AdminLogAction Action { get; set; }

        public string ActionName => Action switch
        {
            AdminLogAction.Verify => "موافقة على محامي",
            AdminLogAction.Reject => "رفض محامي",
            AdminLogAction.Login => "تسجيل دخول",
            AdminLogAction.UpdateProfile => "تحديث الملف الشخصي",
            AdminLogAction.Suspend => "تعليق",
            AdminLogAction.Activate => "تنشيط",
            AdminLogAction.Create => "إنشاء",
            AdminLogAction.Delete => "حذف",
            AdminLogAction.Update => "تعديل",
            AdminLogAction.ChangePassword => "تغيير كلمة مرور",
            AdminLogAction.Export => "تصدير",
            AdminLogAction.ClearCache => "مسح الذاكرة",
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
            AdminLogAction.Create => "➕",
            AdminLogAction.Delete => "🗑️",
            AdminLogAction.Update => "📝",
            AdminLogAction.ChangePassword => "🔒",
            AdminLogAction.Export => "📤",
            AdminLogAction.ClearCache => "🧹",
            _ => "📌"
        };

        public string TargetType { get; set; } = string.Empty;

        public string TargetTypeAr => TargetType switch
        {
            "Lawyer" => "محامي",
            "User" => "مستخدم",
            "Admin" => "أدمن",
            "System" => "نظام",
            "PredefinedContractTemplate" => "قالب عقد",
            _ => "نظام"
        };

        public Guid TargetId { get; set; }

        // تفاصيل الشخص المستهدف
        public object? TargetDetails { get; set; }

        public DateTime Timestamp { get; set; }

        public string TimestampFormatted => Timestamp.ToString("dd MMM yyyy HH:mm");

        public string TimeAgo => GetRelativeTime(Timestamp);

        private static string GetRelativeTime(DateTime dateTime)
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