using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.ReadDTO
{
    public class AdminDashboardDto
    {
        // Stats
        public int TotalUsers { get; set; }
        public int TotalLawyers { get; set; }
        public int PendingVerifications { get; set; }
        public int VerifiedToday { get; set; }       
        // Pending Lawyers
        public List<PendingLawyerDto> PendingLawyers { get; set; } = new();
        
        // Recent Activity
        public List<AdminLogDto> RecentActivity { get; set; } = new();
        
        // Admin Info
        public string AdminName { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public string JobTitle { get; set; } = string.Empty;
    }
}