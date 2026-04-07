using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.ReadDTO
{
    // 4. فقرة
    public class ArticleClauseDto
    {
        public int Id { get; set; }
        public string ClauseNumber { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}


