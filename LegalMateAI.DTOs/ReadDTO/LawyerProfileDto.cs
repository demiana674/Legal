// LegalMateAI.DTOs/ReadDTO/LawyerProfileDto.cs
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
        
        // الجنسية
        public string? Nationality { get; set; }
        
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
        
        // الحالة الأساسية
        public AccountStatus Status { get; set; }
        public string VerificationStatus => Status.ToString();
        public bool IsActive => Status == AccountStatus.Active;
        
        // خصائص التعليق
        public string? SuspensionReason { get; set; }
        public DateTime? SuspendedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? RejectionReason { get; set; }
        
        // إحصائيات
        public int ActiveCases { get; set; }
        public int UpcomingHearings { get; set; }
        public int TotalClients { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // ✅ تخصصات المحامي - كمصفوفة strings للـ Frontend
        public List<string> Specializations { get; set; } = new List<string>();
    }
}