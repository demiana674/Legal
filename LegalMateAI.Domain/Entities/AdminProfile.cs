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
        
        // بيانات شخصية
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        
        // ✅ مشفر في قاعدة البيانات
        public string? NationalId { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        
        // ❌ تم حذف Gender
        // ❌ تم حذف District
        // ❌ تم حذف EmployeeId
        // ❌ تم حذف AccessLevel
        
        // ✅ معرف المحافظة بدل الاسم
        public int? GovernorateId { get; set; }
        public Governorate? Governorate { get; set; }
        
        // ✅ معرف المدينة بدل الاسم
        public int? CityId { get; set; }
        public City? City { get; set; }
        
        public string? Address { get; set; }
        public DateTime? JoinDate { get; set; }
        
        // ✅ مشفر
        public string? AlternativePhone { get; set; }
        
        // ✅ الإحصائيات بتتحدث تلقائياً
        public int TotalVerifiedLawyers { get; set; }
        public int TotalRejectedLawyers { get; set; }
        
        // ✅ آخر نشاط - بيتحدث مع كل إجراء
        public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    }
}