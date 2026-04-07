// LegalMateAI.DTOs/ReadDTO/UserFilterDto.cs
using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class UserFilterDto
    {
        public string? Role { get; set; }
        public string? Status { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        
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