using System;
using System.Collections.Generic;

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
        
        // بيانات مهنية
        public string LicenseNumber { get; set; } = string.Empty;
        public string BarAssociation { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public DateTime? LicenseIssueDate { get; set; }
        public string? PracticeDegree { get; set; }
        
        // الموقع
        public int? GovernorateId { get; set; }
        public string? GovernorateName { get; set; }
        public string? City { get; set; }
        public string? OfficeAddress { get; set; }
        
        // الحالة
        public string VerificationStatus { get; set; } = string.Empty;
        public DateTime? VerifiedAt { get; set; }
        public bool IsActive { get; set; }
        public string? RejectionReason { get; set; }
        
        // إحصائيات
        public int ActiveCases { get; set; }
        public int UpcomingHearings { get; set; }
        public int TotalClients { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // تخصصات المحامي
        public List<SpecializationDto> Specializations { get; set; } = new List<SpecializationDto>();
    }
}