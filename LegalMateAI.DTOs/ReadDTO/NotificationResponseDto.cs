using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    // 10. Notification Response
    public class NotificationResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string TypeIcon => Type switch
        {
            NotificationType.Info => "ℹ️",
            NotificationType.Success => "✅",
            NotificationType.Warning => "⚠️",
            NotificationType.Error => "❌",
            _ => "🔔"
        };
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted => GetRelativeTime(CreatedAt);
        public string? ActionUrl { get; set; }

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

