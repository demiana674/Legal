// LegalMateAI.DTOs/UpdateDTO/UpdateLawyerProfileDto.cs
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
        public string? City { get; set; }
        public string? OfficeAddress { get; set; }
        
        // ✅ Specializations - قائمة بأسماء التخصصات
        public List<string> Specializations { get; set; } = new List<string>();
        
        // للتوافق مع الإصدارات القديمة
        public List<Guid>? SpecialtyIds { get; set; }
    }
}