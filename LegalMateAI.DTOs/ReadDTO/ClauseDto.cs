using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
// LegalMateAI.DTOs/ReadDTO/ClauseDto.cs
namespace LegalMateAI.DTOs.ReadDTO
{
    public class ClauseDto
    {
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public string Interpretation { get; set; } = string.Empty;
    }
}