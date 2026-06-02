// LegalMateAI.Domain/Entities/LawyerSpecialization.cs
using System;

namespace LegalMateAI.Domain.Entities
{
    public class LawyerSpecialization
    {
        public Guid Id { get; set; }
        public Guid LawyerId { get; set; }
        public int SpecializationId { get; set; }  // ✅ هذا يربط بـ LawyerSpecialty.Id
        public bool IsPrimary { get; set; }
        public int CasesCount { get; set; }
        
        public LawyerProfile Lawyer { get; set; } = null!;
        public LawyerSpecialty Specialization { get; set; } = null!;  // ✅ استخدم LawyerSpecialty
    }
}