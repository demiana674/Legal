using System;
using System.Collections.Generic;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateLawyerProfileDto
    {
        // Basic Info
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AlternativePhone { get; set; }
        public string? NationalId { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        
        // Professional Info
        public string? LicenseNumber { get; set; }
        public string? BarAssociation { get; set; }
        public int? YearsOfExperience { get; set; }
        public DateTime? LicenseIssueDate { get; set; }
        public string? PracticeDegree { get; set; }
        
        // Location
        public int? GovernorateId { get; set; }
        public int? CityId { get; set; }
        public string? Governorate { get; set; }
        public string? City { get; set; }
        public string? OfficeAddress { get; set; }
        
        // ✅ المهارات والكفاءات (قائمة)
        public List<string> Skills { get; set; } = new List<string>();
        
        // ✅ المهارات كنص (مفصول بفواصل)
        public string? SkillsText { get; set; }
        
        // ✅ عدد المحاج المعتمدة
        public int? ApprovedLitigationsCount { get; set; }
        
        // Specializations
        public List<string> Specializations { get; set; } = new List<string>();
        
        // Certificates
        public List<CertificateInputDto>? Certificates { get; set; }
        
        // Legacy
        public List<int>? SpecialtyIds { get; set; }
    }

    public class CertificateInputDto
    {
        public string Name { get; set; } = string.Empty;
        public string IssuingOrganization { get; set; } = string.Empty;
        public int Year { get; set; }
        public string? FileUrl { get; set; }
    }
}