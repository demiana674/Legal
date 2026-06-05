using System.ComponentModel.DataAnnotations.Schema;

namespace LegalMateAI.Domain.Entities
{
    public class UserProfile
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public virtual User? User { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AlternativePhone { get; set; }
        public string? NationalId { get; set; }
        public string? Nationality { get; set; }
        public DateTime? DateOfBirth { get; set; }

        // ✅ حقول نصية للمحافظة والمدينة
        public string? Governorate { get; set; }
        public string? City { get; set; }

        // ✅ للتوافق مع القديم (اختياري)
        public int? GovernorateId { get; set; }
        public int? CityId { get; set; }

        [ForeignKey("GovernorateId")]
        public virtual Governorate? GovernorateNavigation { get; set; }

        [ForeignKey("CityId")]
        public virtual City? CityNavigation { get; set; }

        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastProfileUpdate { get; set; }
    }
}