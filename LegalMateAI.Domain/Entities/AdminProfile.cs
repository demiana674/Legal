using System;

namespace LegalMateAI.Domain.Entities
{
    public class AdminProfile
    {
        public Guid Id { get; set; }
        public Guid AdminId { get; set; }
        public Admin Admin { get; set; } = null!;
        
        public string? ProfilePictureUrl { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        
        // 🆕 بيانات شخصية
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? NationalId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Nationality { get; set; }
        
        // 🆕 موقع
        public string? Governorate { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Address { get; set; }
        
        // 🆕 توظيف
        public string? EmployeeId { get; set; }
        public string? AccessLevel { get; set; }
        public DateTime? JoinDate { get; set; }
        public string? AlternativePhone { get; set; }
        
        // إحصائيات
        public int TotalVerifiedLawyers { get; set; }
        public int TotalRejectedLawyers { get; set; }
        public DateTime LastActiveAt { get; set; }
    }
}