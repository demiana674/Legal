using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawyerProfileSpecialtyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int YearsOfExperience { get; set; }
    }
}