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
    // 12. أحكام المحاكم (للبحث القانوني المتقدم)
    public class CourtRuling
    {
        public int Id { get; set; }
        public string RulingNumber { get; set; } = string.Empty;
        public int CourtId { get; set; }
        public EgyptianCourt Court { get; set; } = null!;
        public DateTime RulingDate { get; set; }
        public string CaseNumber { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
        public string? LegalPrinciple { get; set; } // المبدأ القانوني
        public int[]? RelatedLawIds { get; set; }
        public int[]? RelatedArticleIds { get; set; }
    }
}