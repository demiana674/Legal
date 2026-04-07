using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawSearchResultDto
    {
        // public int Id { get; set; }
        public string Id { get; set; } = string.Empty;  // ✅ وليس int
        public string LawNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "Law"; // Law, Article, Clause
        public string? Context { get; set; } // النص الذي ظهر فيه البحث
        public double Relevance { get; set; }
        public string RelevanceFormatted => $"{Relevance:P0}";
        public string Url { get; set; } = string.Empty;
    }
}


