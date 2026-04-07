using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.ReadDTO
{
    // 2. مادة قانونية (مختصرة)
    public class LawArticleBriefDto
    {
        public int Id { get; set; }
        public int ArticleNumber { get; set; }
        public string? Title { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public bool IsActive { get; set; }
    }
}


