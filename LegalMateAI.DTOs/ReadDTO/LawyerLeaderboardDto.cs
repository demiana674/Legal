using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawyerLeaderboardDto
    {
        public Guid LawyerId { get; set; }
        public string LawyerName { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public int TotalCases { get; set; }
        public double SuccessRate { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public decimal Revenue { get; set; }
    }
}