using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class PendingLawyerDto
    {
        public Guid UserId { get; set; }           // ✅ UserID مش Id
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        
        // ❌ متحاولش تعمل set ليها - هي calculated
        public string FullName => $"{FirstName} {LastName}";
        
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string BarAssociation { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}