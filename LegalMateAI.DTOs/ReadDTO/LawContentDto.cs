using System.Collections.Generic;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawContentDto
    {
        public string? Description { get; set; }
        public List<string> SearchKeywords { get; set; } = new();
    }
}