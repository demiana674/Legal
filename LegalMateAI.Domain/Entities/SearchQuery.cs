using LegalMateAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
        public class SearchQuery
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Query { get; set; } = string.Empty;
        public string? ProcessedIntent { get; set; }
        public string[]? ExtractedConcepts { get; set; }
        public int ResultCount { get; set; }
        public DateTime SearchedAt { get; set; }
        
        public User User { get; set; } = null!;
    }
}