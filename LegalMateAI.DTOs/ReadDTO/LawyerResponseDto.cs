// LegalMateAI.DTOs/ReadDTO/LawyerResponseDto.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawyerResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AlternativePhone { get; set; }
        
        public string NationalId { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string ProfilePicture { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string BarAssociation { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        
        //  الحالة الأساسية
        public AccountStatus Status { get; set; }
        
        //  للتوافق مع الفرونت القديم - نفس قيمة Status
        public string VerificationStatus => Status.ToString();
        
        //  IsActive محسوبة
        public bool IsActive => Status == AccountStatus.Active;
        
        //  خصائص التعليق
        public string? SuspensionReason { get; set; }
        public DateTime? SuspendedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        
        public DateTime? VerifiedAt { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? VerifiedAtFormatted { get; set; }
        
        public string? RejectionReason { get; set; }
        public double Rating { get; set; }
        public int TotalReviews { get; set; }
        public int? GovernorateId { get; set; }
        public string? GovernorateName { get; set; }
        public string? City { get; set; }
        public string? OfficeAddress { get; set; }
        
        public string? DateOfBirth { get; set; }
        public string? CreatedAt { get; set; }
        
        public List<LawyerProfileSpecialtyDto> Specialties { get; set; } = new();
        public List<CertificateDto> Certificates { get; set; } = new();
    }
}