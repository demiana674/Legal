// LegalMateAI.Domain/Entities/UserProfile.cs
using System;
using System.Collections.Generic;

namespace LegalMateAI.Domain.Entities
{
    public class UserProfile
    {
        public Guid Id { get; set; }
        
        // العلاقة مع User
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        
        // المعلومات الأساسية
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? FullName => $"{FirstName} {LastName}";
        public string? ProfilePictureUrl { get; set; }
        
        // معلومات الاتصال
        public string? PhoneNumber { get; set; }
        public string? AlternativePhone { get; set; }
        public string? Email { get; set; }
        
        // الموقع
        public int? GovernorateId { get; set; }
        public virtual Governorate? Governorate { get; set; }
        public int? CityId { get; set; }
        public virtual City? City { get; set; }
        public string? Address { get; set; }
        public string? NationalId { get; set; }
        
        // إعدادات المستخدم
        public string? Theme { get; set; } = "dark";
        public bool IsProfilePublic { get; set; } = false;
        
        // إحصائيات
        public int ProfileViews { get; set; }
        public DateTime LastProfileUpdate { get; set; }
        
        // التواريخ
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // ❌ إزالة هذا السطر (مكرر)
        // public int? GovernorateId1 { get; set; }
        
        // العلاقات
        public ICollection<UserDocument> Documents { get; set; } = new List<UserDocument>();
        public ICollection<UserSocialLink> SocialLinks { get; set; } = new List<UserSocialLink>();
        public UserPreferences? Preferences { get; set; }
    }
}