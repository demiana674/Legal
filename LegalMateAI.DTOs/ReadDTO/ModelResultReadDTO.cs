using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ModelResultReadDTO
    {
        public int ResultID { get; set; }
        public int ModelID { get; set; }
   
        public string InputData { get; set; } = string.Empty;
        public string OutputData { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public double? ConfidenceScore { get; set; }

    }
}
