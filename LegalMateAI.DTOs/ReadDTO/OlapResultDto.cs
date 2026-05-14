using System;
using System.Collections.Generic;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class OlapResultDto
    {
        public List<string> Dimensions { get; set; } = new();
        public List<string> Measures { get; set; } = new();
        public List<Dictionary<string, object>> Data { get; set; } = new();
        public Dictionary<string, object> Aggregates { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}