using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    // 9. Search Response
    public class SearchResponseDto
    {
        public string Query { get; set; } = string.Empty;
        public string? ProcessedIntent { get; set; }
        public int TotalResults { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalResults / (double)PageSize);
        
        public List<SearchResultDto> Results { get; set; } = new();
    }
}


