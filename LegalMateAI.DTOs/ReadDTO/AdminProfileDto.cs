using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class AdminProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AlternativePhone { get; set; }
        public string? ProfilePicture { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string? AccessLevel { get; set; }
        
        // بيانات شخصية
        public string? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? NationalId { get; set; }
        public string? EmployeeId { get; set; }
        
        // موقع
        public string? Governorate { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Address { get; set; }
        
        // تواريخ
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? JoinDateFormatted { get; set; }
        public int? TotalMonthsActive { get; set; }
        
        // حالة
        public string? Status { get; set; }
        public bool IsOnline { get; set; }
        
        // إحصائيات Dashboard
        public int TotalUsers { get; set; }
        public int TotalLawyers { get; set; }
        public int PendingVerifications { get; set; }
        public int VerifiedToday { get; set; }
        public int TotalVerifiedLawyers { get; set; }
        public int TotalRejectedLawyers { get; set; }
        
        // صلاحيات
        public bool CanManageUsers { get; set; }
        public bool CanVerifyLawyers { get; set; }
        public bool CanManageSystem { get; set; }
        public bool CanExportData { get; set; }
    }
}