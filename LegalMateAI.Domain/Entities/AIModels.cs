using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Entities
{
    public class AIModels
    {
        [Key]
        public int ModelID { get; set; }
        [Required]
        [MaxLength(200)]
        public string ModelName { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? Description { get; set; }
        [Required]
        [MaxLength(50)]
        public string Version { get; set; } = "1.0";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public ModelType ModelType { get; set; } = ModelType.Other;
        [MaxLength(200)]
        public string? TrainedBy { get; set; }

        public ICollection<ModelResults>? ModelResults { get; set; } = new List<ModelResults>();
    }
    public enum ModelType
    {
        NLP,             // معالجة اللغة الطبيعية
        IR,              // استرجاع المعلومات
        Summarization,   // التلخيص
        Classification,  // التصنيف
        Recommendation,  // التوصية
        Chatbot,         // محادثة
        Other            // أنواع أخرى
    }
}
