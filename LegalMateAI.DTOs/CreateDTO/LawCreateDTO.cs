using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class LawCreateDTO
    {
           public string Title { get; set; }= string.Empty;

        public string? SourceURL { get; set; }
        public string Category { get; set; } = "General";
        public string? IssuedBy { get; set; }
    }
}