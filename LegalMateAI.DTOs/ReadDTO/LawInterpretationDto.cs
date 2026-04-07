using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.ReadDTO
{
    // 6. تفسير قانوني
    public class LawInterpretationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public InterpretationSource Source { get; set; }
        public string SourceName => Source switch
        {
            InterpretationSource.CourtOfCassation => "محكمة النقض",
            InterpretationSource.ConstitutionalCourt => "المحكمة الدستورية",
            InterpretationSource.LegalScholars => "فقهاء القانون",
            InterpretationSource.AcademicResearch => "بحث أكاديمي",
            InterpretationSource.AdminAdded => "إضافة من النظام",
            _ => Source.ToString()
        };
        public string? SourceReference { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}


