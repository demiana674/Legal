using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserImage { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted => CreatedAt.ToString("dd MMM yyyy");
        public string TimeAgo => GetRelativeTime(CreatedAt);

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