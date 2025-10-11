using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class ModelResultCreateDTO
    {
        [Required]
        public int ModelID { get; set; }

        [Required]
        [MaxLength(2000)]
        public string InputData { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? OutputData { get; set; }

        [Range(0, 1)]
        public double? ConfidenceScore { get; set; }
    }
}
