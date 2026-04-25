// LegalMateAI.Domain/Entities/UserProfile.cs
using System;
using System.Collections.Generic;

namespace LegalMateAI.Domain.Entities
{
    public class UserProfile
    {
        public Guid Id { get; set; }
        
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? FullName => $"{FirstName} {LastName}";
        public string? ProfilePictureUrl { get; set; }
        
        public string? PhoneNumber { get; set; }
        public string? AlternativePhone { get; set; }
        public string? Email { get; set; }
        
        public int? GovernorateId { get; set; }
        public virtual Governorate? Governorate { get; set; }
        public int? CityId { get; set; }
        public virtual City? City { get; set; }
        public string? Address { get; set; }
        public string? NationalId { get; set; }
        public string? Nationality { get; set; }
        public DateTime? DateOfBirth { get; set; }
        
        public string? Theme { get; set; } = "dark";
        public bool IsProfilePublic { get; set; } = false;
        
        public int ProfileViews { get; set; }
        public DateTime LastProfileUpdate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        public ICollection<UserDocument> Documents { get; set; } = new List<UserDocument>();
        public ICollection<UserSocialLink> SocialLinks { get; set; } = new List<UserSocialLink>();
        public UserPreferences? Preferences { get; set; }
    }
}