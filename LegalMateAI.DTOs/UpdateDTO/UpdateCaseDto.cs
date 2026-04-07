// LegalMateAI.DTOs/UpdateDTO/UpdateCaseDto.cs
using System;
using System.ComponentModel.DataAnnotations;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateCaseDto
    {
        [StringLength(200)]
        public string? Title { get; set; }
        
        public string? Description { get; set; }
        
        public string? Court { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? NextHearingDate { get; set; }
        
        public CaseStatus? Status { get; set; }
        
        public CasePriority? Priority { get; set; }
        
        public string? CaseType { get; set; }
    }
}