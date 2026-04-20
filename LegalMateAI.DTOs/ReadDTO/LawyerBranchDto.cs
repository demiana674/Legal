// LegalMateAI.DTOs/ReadDTO/LawyerBranchDto.cs
using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawyerBranchDto
    {
        public Guid Id { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int? GovernorateId { get; set; }
        public string? GovernorateName { get; set; }
        public string? City { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
    }
}