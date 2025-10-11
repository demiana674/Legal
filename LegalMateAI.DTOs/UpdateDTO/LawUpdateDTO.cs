using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LegalMateAI.DTOs.UpdateDTO
{
    public class LawUpdateDTO
    {
        public string? Title { get; set; }
        public string? SourceURL { get; set; }
        public string? Category { get; set; }
        public string? IssuedBy { get; set; }
    }
}