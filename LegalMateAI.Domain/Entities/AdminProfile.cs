using System;

namespace LegalMateAI.Domain.Entities
{
    public class AdminProfile
    {
        public Guid Id { get; set; }
        public Guid AdminId { get; set; }
        public Admin Admin { get; set; } = null!;
        
        public string? ProfilePictureUrl { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        
        public int TotalVerifiedLawyers { get; set; }
        public int TotalRejectedLawyers { get; set; }
        public DateTime LastActiveAt { get; set; }
    }
}