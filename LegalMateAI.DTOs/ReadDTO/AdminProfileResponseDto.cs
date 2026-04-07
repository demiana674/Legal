// LegalMateAI.DTOs/ReadDTO/AdminProfileResponseDto.cs
using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class AdminProfileResponseDto
    {
        public Guid Id { get; set; }
        public Guid AdminId { get; set; }
        
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        
        public string? PhoneNumber { get; set; }
        public string? AlternativePhone { get; set; }
        public string? Email { get; set; }
        
        public int? GovernorateId { get; set; }
        public string? GovernorateName { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        
        public string? Department { get; set; }
        public string? JobTitle { get; set; }
        public DateTime? HireDate { get; set; }
        public string? HireDateFormatted => HireDate?.ToString("dd MMM yyyy");
        public string? EmployeeId { get; set; }
        
        public int ProfileViews { get; set; }
        public int ActionsCount { get; set; }
        public DateTime LastActiveAt { get; set; }
        public string LastActiveAtFormatted => GetRelativeTime(LastActiveAt);
        
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted => CreatedAt.ToString("dd MMM yyyy");
        public DateTime? UpdatedAt { get; set; }
        
        private string GetRelativeTime(DateTime dateTime)
        {
            var diff = DateTime.UtcNow - dateTime;
            if (diff.TotalMinutes < 1) return "الآن";
            if (diff.TotalMinutes < 60) return $"منذ {diff.Minutes} دقيقة";
            if (diff.TotalHours < 24) return $"منذ {diff.Hours} ساعات";
            if (diff.TotalDays < 7) return $"منذ {diff.Days} أيام";
            return dateTime.ToString("dd MMM yyyy");
        }
    }
}