using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class AIModelCreateDTO
    {
        public string ModelName { get; set; }= string.Empty;
        public string? Description { get; set; }
        public string Version { get; set; }= "1.0";

    }
}