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
    // 8. التفسيرات القانونية (للمساعدة في فهم النص)
    public class LawInterpretation
    {
        public int Id { get; set; }
        public int? LawId { get; set; }
        public EgyptianLaw? Law { get; set; }
        public int? ArticleId { get; set; }
        public LawArticle? Article { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty; // شرح مبسط
        public InterpretationSource Source { get; set; } // نقض، فقه، دكتوراه
        public string? SourceReference { get; set; } // "حكم محكمة النقض 1234 لسنة 2020"
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedByAdminId { get; set; }



    }
}