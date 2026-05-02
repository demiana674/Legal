// LegalMateAI.DTOs/ReadDTO/UnifiedLogFilterDto.cs
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class UnifiedLogFilterDto
    {
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        
        // 🔍 Search (بحث نصي في الاسم والإيميل والإجراء)
        public string? SearchTerm { get; set; }
        
        // Filters by ID
        public Guid? UserId { get; set; }        // مستخدم/محامي محدد
        public Guid? AdminId { get; set; }       // أدمن محدد
        
        // Filters by Type
        public string? TargetType { get; set; }  // "User", "Lawyer", "Admin", "System"
        public UserRole? Role { get; set; }      // دور المستخدم المستهدف
        
        // Filters by Action
        public AdminLogAction? Action { get; set; }
        
        // Date Range
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}