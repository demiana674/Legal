using System;
using System.Collections.Generic;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawyerProfileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? AlternativePhone { get; set; }
        public string? ProfilePicture { get; set; }
        public string? NationalId { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public string BarAssociation { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public DateTime? LicenseIssueDate { get; set; }
        public string? PracticeDegree { get; set; }
        public int? GovernorateId { get; set; }
        public string? GovernorateName { get; set; }
        public string? City { get; set; }
        public string? OfficeAddress { get; set; }
        public AccountStatus Status { get; set; }
        public string VerificationStatus => Status.ToString();
        public bool IsActive => Status == AccountStatus.Active;
        public string? SuspensionReason { get; set; }
        public DateTime? SuspendedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? RejectionReason { get; set; }
        public int ActiveCases { get; set; }
        public int UpcomingHearings { get; set; }
        public int TotalClients { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // ✅ المهارات والكفاءات (قائمة)
        public List<string> Skills { get; set; } = new List<string>();
        
        // ✅ عدد المحاج المعتمدة
        public int ApprovedLitigationsCount { get; set; }
        
        // ✅ عدد الموكلين
        public int ClientsCount { get; set; }
        
        // ✅ عدد التقييمات
        public int TotalReviews { get; set; }
        
        // ✅ متوسط التقييم
        public double AverageRating { get; set; }
        
        public List<string> Specializations { get; set; } = new List<string>();
        public List<CertificateDto> Certificates { get; set; } = new List<CertificateDto>();
    }
}