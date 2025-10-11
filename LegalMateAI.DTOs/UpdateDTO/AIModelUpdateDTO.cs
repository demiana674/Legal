using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class AIModelUpdateDTO
    {
        
        public string? ModelName { get; set; }
        public string? Version { get; set; }
        public string? Description { get; set; }
        
    }
}