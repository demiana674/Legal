// LegalMateAI.DTOs/ReadDTO/LawyerFilterDto.cs
using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawyerFilterDto
    {
        public string? Status { get; set; }
        public string? SearchTerm { get; set; }
        public int? GovernorateId { get; set; }
        public int? SpecializationId { get; set; }
        public string? City { get; set; }
        public int? MinExperience { get; set; }
        public double? MinRating { get; set; }
        public bool OnlyAvailable { get; set; }
        public DateTime? PreferredDate { get; set; }
        
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