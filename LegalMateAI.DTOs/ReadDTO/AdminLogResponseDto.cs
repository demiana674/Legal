// LegalMateAI.DTOs/ReadDTO/AdminLogResponseDto.cs
using System;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class AdminLogResponseDto
    {
        public Guid Id { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public AdminLogAction Action { get; set; }
        
        public string ActionName => Action switch
        {
            AdminLogAction.Login => "تسجيل دخول",
            AdminLogAction.Verify => "موافقة على محامي",
            AdminLogAction.Reject => "رفض محامي",
            AdminLogAction.Suspend => "تعليق",
            AdminLogAction.Activate => "تنشيط",
            AdminLogAction.UpdateProfile => "تحديث ملف",
            _ => Action.ToString()
        };
        
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string TimestampFormatted => Timestamp.ToString("dd MMM yyyy HH:mm");
        public string? IpAddress { get; set; }
        public string? Details { get; set; }
    }
}