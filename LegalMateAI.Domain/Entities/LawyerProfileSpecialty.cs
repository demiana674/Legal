// LegalMateAI.Domain/Entities/LawyerProfileSpecialty.cs
using System;

namespace LegalMateAI.Domain.Entities
{
    public class LawyerProfileSpecialty
    {
        public Guid Id { get; set; }
        public Guid LawyerId { get; set; }
        public int SpecialtyId { get; set; }
        public bool IsPrimary { get; set; }
        public int YearsOfExperience { get; set; }
        
        public LawyerProfile Lawyer { get; set; } = null!;
        public LawyerSpecialty Specialty { get; set; } = null!;
    }
}