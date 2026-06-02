// LegalMateAI.Domain/Entities/LawyerProfile.cs
using System;
using System.Collections.Generic;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    public class LawyerProfile
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        
        // Professional Information
        public string? LicenseNumber { get; set; }
        public string? BarAssociation { get; set; }
        public DateTime? LicenseIssueDate { get; set; }
        public string? PracticeDegree { get; set; }
        public int? YearsOfExperience { get; set; }
        
        public string? PhoneNumber { get; set; }
        public string? AlternativePhone { get; set; }
        
        // Location
        public int? GovernorateId { get; set; }
        public Governorate? Governorate { get; set; }
        public int? CityId { get; set; }
        public City? City { get; set; }
        public string? OfficeAddress { get; set; }
        
        //  بيانات الموافقة والرفض (تبقى هنا)
        public DateTime? VerifiedAt { get; set; }
        public string? RejectionReason { get; set; }
        
        //  خصائص التعليق
        public string? SuspensionReason { get; set; }
        public DateTime? SuspendedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Relationships
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        public ICollection<LawyerAvailability> Availabilities { get; set; } = new List<LawyerAvailability>();
        public ICollection<LawyerReview> Reviews { get; set; } = new List<LawyerReview>();
         public virtual ICollection<LawyerSpecialization> Specializations { get; set; } = new List<LawyerSpecialization>();
	
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<LawyerProfileSpecialty> Specialties { get; set; } = new List<LawyerProfileSpecialty>();
    }
}