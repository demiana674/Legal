using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawyerSuggestionDto
    {
        public Guid LawyerId { get; set; }
        public string LawyerName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int CasesCount { get; set; }
        public double MatchScore { get; set; }
        public string MatchScoreFormatted => $"{MatchScore:P0}";
    }
}

