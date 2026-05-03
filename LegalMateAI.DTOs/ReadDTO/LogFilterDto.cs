using System;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LogFilterDto
    {
        // public string? AdminId { get; set; } 
        public Guid? UserId { get; set; }
        public string? UserType { get; set; }
        public AdminLogAction? Action { get; set; }
        public String? TargetType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Status { get; set; }
        /// <summary>
        /// بحث نصي في الاسم أو البريد
        /// </summary>
        public string? SearchTerm { get; set; }
        
        private int _page = 1;
        public int Page { get => _page; set => _page = value < 1 ? 1 : value; }
        
        private int _pageSize = 50;
        public int PageSize { get => _pageSize; set => _pageSize = value < 1 ? 50 : (value > 500 ? 500 : value); }
    }
}