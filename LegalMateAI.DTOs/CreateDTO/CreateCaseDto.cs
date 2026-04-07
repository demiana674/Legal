// LegalMateAI.DTOs/CreateDTO/CreateCaseDto.cs
using System;
using System.ComponentModel.DataAnnotations;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateCaseDto
    {
        [Required(ErrorMessage = "عنوان القضية مطلوب")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "معرف الموكل مطلوب")]
        public Guid ClientId { get; set; }
        
        public string? Court { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? NextHearingDate { get; set; }
        
        public CaseStatus Status { get; set; } = CaseStatus.Pending;
        public CasePriority Priority { get; set; } = CasePriority.Medium;
        
        public string? CaseType { get; set; }
    }
}