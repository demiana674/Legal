using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using LegalMateAI.DTOs.ReadDTO;
namespace LegalMateAI.DTOs.ReadDTO
{
    // 3. مادة قانونية (كاملة)
    public class LawArticleDetailedDto : LawArticleBriefDto
    {
        public int LawId { get; set; }
        public string LawName { get; set; } = string.Empty;
        public DateTime? AmendedAt { get; set; }
        public string? AmendmentDescription { get; set; }
        public List<ArticleClauseDto> Clauses { get; set; } = new();
        public List<LawInterpretationDto> Interpretations { get; set; } = new();
    }
}

