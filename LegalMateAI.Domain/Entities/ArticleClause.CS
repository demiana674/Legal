using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Enums;
namespace LegalMateAI.Domain.Entities
{
    // 5. فقرات المادة
    public class ArticleClause
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }
        public LawArticle Article { get; set; } = null!;
        public string ClauseNumber { get; set; } = string.Empty; // "أ", "ب", "1", "2"
        public string Content { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
