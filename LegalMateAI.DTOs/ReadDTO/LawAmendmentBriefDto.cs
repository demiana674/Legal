using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.ReadDTO
{
    // 5. تعديل قانوني
    public class LawAmendmentBriefDto
    {
        public int Id { get; set; }
        public string AmendmentNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime AmendmentDate { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string? Description { get; set; }
    }
}


