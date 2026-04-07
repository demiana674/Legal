// LegalMateAI.DTOs/ReadDTO/AdminProfileDto.cs
using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class AdminProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfilePicture { get; set; }
        public string? JobTitle { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        
        // إحصائيات Dashboard
        public int TotalUsers { get; set; }
        public int TotalLawyers { get; set; }
        public int PendingVerifications { get; set; }
        public int VerifiedToday { get; set; }
    }
}