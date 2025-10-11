using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Entities
{
    public class ModelResults
    {
        [Key]
        public int ResultID { get; set; }
        [Required]
        public int ModelID { get; set; }
        [Required]
        [MaxLength(4000)]
        public string InputData { get; set; }= string.Empty;
        [Required]   
        public string OutputData { get; set; }= string.Empty;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        [Range(0, 1)]
        public double? ConfidenceScore { get; set; }
        public AIModels AIModel { get; set; }= null!;
    }
}