using LegalMateAI.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalMateAI.Domain.Entities
{
    public class LawyerProfile
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public virtual User? User { get; set; }

        public string? LicenseNumber { get; set; }
        public string? BarAssociation { get; set; }
        public DateTime? LicenseIssueDate { get; set; }
        public string? PracticeDegree { get; set; }
        public int? YearsOfExperience { get; set; }

        // حقول نصية للمحافظة والمدينة
        public string? Governorate { get; set; }
        public string? City { get; set; }

        // للتوافق مع القديم (اختياري)
        public int? GovernorateId { get; set; }
        public int? CityId { get; set; }

        [ForeignKey("GovernorateId")]
        public virtual Governorate? GovernorateNavigation { get; set; }

        [ForeignKey("CityId")]
        public virtual City? CityNavigation { get; set; }

        public string? OfficeAddress { get; set; }
        public string? AlternativePhone { get; set; }
        public string? PhoneNumber { get; set; }

        public AccountStatus VerificationStatus { get; set; }
        public string? RejectionReason { get; set; }
        public string? SuspensionReason { get; set; }
        public DateTime? SuspendedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ✅ المهارات والكفاءات (مفصولة بفواصل)
        public string? Skills { get; set; }

        // ✅ عدد المحاج المعتمدة
        public int ApprovedLitigationsCount { get; set; }

        // ✅ عدد الموكلين
        public int ClientsCount { get; set; }

        // العلاقات
        public virtual ICollection<LawyerSpecialization>? Specializations { get; set; }
        public virtual ICollection<LawyerProfileSpecialty>? Specialties { get; set; }
        public virtual ICollection<Certificate>? Certificates { get; set; }
        public virtual ICollection<LawyerReview>? Reviews { get; set; }
    }
}