// LegalMateAI.DTOs/ReadDTO/UnifiedLogsResponseDto.cs
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class UnifiedLogsResponseDto
    {
        public List<UnifiedLogDto> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
    
    public class UnifiedLogDto
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimestampFormatted => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        
        // Admin who performed the action
        public Guid AdminId { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        
        // Action details
        public AdminLogAction Action { get; set; }
        public string ActionName => Action.ToString();
        public string ActionDescription => GetActionDescription();
        
        // Target details (User/Lawyer/Admin)
        public string TargetType { get; set; } = string.Empty;
        public string TargetTypeAr => TargetType switch
        {
            "User" => "مستخدم",
            "Lawyer" => "محامي",
            "Admin" => "أدمن",
            "System" => "نظام",
            _ => TargetType
        };
        public Guid TargetId { get; set; }
        public string? TargetName { get; set; }
        public string? TargetEmail { get; set; }
        public UserRole? TargetRole { get; set; }
        public string? TargetRoleAr => TargetRole?.ToString() switch
        {
            "User" => "مستخدم",
            "Lawyer" => "محامي",
            "Admin" => "أدمن",
            _ => TargetRole?.ToString()
        };
        
        // Additional info
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        
        private string GetActionDescription()
        {
            return Action switch
            {
                AdminLogAction.Login => "تسجيل دخول",
                AdminLogAction.Logout => "تسجيل خروج",
                AdminLogAction.Create => "إنشاء",
                AdminLogAction.Update => "تحديث",
                AdminLogAction.Delete => "حذف",
                AdminLogAction.Verify => "الموافقة على محامي",
                AdminLogAction.Reject => "رفض محامي",
                AdminLogAction.Suspend => "تعليق",
                AdminLogAction.Activate => "تنشيط",
                AdminLogAction.UpdateProfile => "تحديث ملف شخصي",
                AdminLogAction.ChangePassword => "تغيير كلمة مرور",
                AdminLogAction.ClearCache => "مسح الذاكرة المؤقتة",
                _ => Action.ToString()
            };
        }
    }
}