// LegalMateAI.DTOs/ReadDTO/CaseFilterDto.cs
using System;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class CaseFilterDto
    {
        public Guid? ClientId { get; set; }
        public Guid? LawyerId { get; set; }
        public CaseStatus? Status { get; set; }
        public CasePriority? Priority { get; set; }
        public string? CaseType { get; set; }
        public string? Court { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? NextHearingFrom { get; set; }
        public DateTime? NextHearingTo { get; set; }
        
        private int _page = 1;
        public int Page 
        { 
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }
        
        private int _pageSize = 20;
        public int PageSize 
        { 
            get => _pageSize;
            set => _pageSize = value < 1 ? 20 : (value > 100 ? 100 : value);
        }
    }
}