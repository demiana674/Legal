using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class SearchResultDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Document, Case, Law, Article
        public string Title { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public double Relevance { get; set; }
        public string RelevanceFormatted => $"{Relevance:P0}";
        public string? Url { get; set; }
        public DateTime? Date { get; set; }
    }
}


