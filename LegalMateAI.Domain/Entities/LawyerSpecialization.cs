// LegalMateAI.Domain/Entities/LawyerSpecialization.cs
using System;

namespace LegalMateAI.Domain.Entities
{
    public class LawyerSpecialization
    {
        public Guid Id { get; set; }
        public Guid LawyerId { get; set; }
        public int SpecializationId { get; set; }
        public bool IsPrimary { get; set; }
        public int CasesCount { get; set; }
        
        public LawyerProfile Lawyer { get; set; } = null!;
        public LegalSpecialization Specialization { get; set; } = null!;
    }
}