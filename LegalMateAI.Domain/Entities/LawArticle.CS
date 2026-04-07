using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Enums;
using LegalMateAI.Domain.Entities;
namespace LegalMateAI.Domain.Entities
{
    // 4. المواد القانونية
    public class LawArticle
    {
        public int Id { get; set; }
        public int LawId { get; set; }
        public EgyptianLaw Law { get; set; } = null!;
        public int ArticleNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty; // النص الكامل
        public string? Summary { get; set; } // ملخص للمستخدم العادي
        public bool IsActive { get; set; } = true;
        public DateTime? AmendedAt { get; set; }
        public string? AmendmentDescription { get; set; }
        public ICollection<LawInterpretation> Interpretations { get; set; }  = new List<LawInterpretation>();

        public ICollection<ArticleClause> Clauses { get; set; } = new List<ArticleClause>();
        public ICollection<ArticleVersion> Versions { get; set; } = new List<ArticleVersion>();
    }
}
