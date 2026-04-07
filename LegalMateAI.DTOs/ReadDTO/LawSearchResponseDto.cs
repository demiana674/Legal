using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.ReadDTO
{
    // 7. بحث في القوانين
    public class LawSearchResponseDto
    {
        public string Query { get; set; } = string.Empty;
        public int TotalResults { get; set; }
        public List<LawSearchResultDto> Results { get; set; } = new();
    }
}