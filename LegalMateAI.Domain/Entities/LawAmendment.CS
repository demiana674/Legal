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
    // 7. التعديلات القانونية
    public class LawAmendment
    {
        public int Id { get; set; }
        public int LawId { get; set; }
        public EgyptianLaw Law { get; set; } = null!;
        public string AmendmentNumber { get; set; } = string.Empty; // "قانون 10 لسنة 2020"
        public string Title { get; set; } = string.Empty;
        public DateTime AmendmentDate { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public int[]? AffectedArticles { get; set; } // المواد المتأثرة
    }
}
