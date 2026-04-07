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
    // 10. الكلمات المفتاحية (للبحث الذكي)
    public class LawKeyword
    {
        public int Id { get; set; }
        public int LawId { get; set; }
        public EgyptianLaw Law { get; set; } = null!;
        public string Keyword { get; set; } = string.Empty;
        public int Weight { get; set; } = 1; // أهمية الكلمة
    }
}
