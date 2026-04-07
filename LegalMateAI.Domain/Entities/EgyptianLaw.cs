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

// 1. القانون الرئيسي
    public class EgyptianLaw
    {
        public int Id { get; set; }
        public string LawNumber { get; set; } = string.Empty; // مثلاً: "قانون 131 لسنة 1948"
        public string Title { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public string? ShortTitle { get; set; } // "القانون المدني"
        public int Year { get; set; }
        public LawCategory Category { get; set; }
        public LawStatus Status { get; set; } // ساري، ملغي، معدل
        public string? Description { get; set; }
        public string? SourceUrl { get; set; } // رابط للجريدة الرسمية
        public DateTime PublishedAt { get; set; }
        public DateTime? LastAmendedAt { get; set; }
        public int ViewCount { get; set; }
            public static List<EgyptianLaw> GetBaseLaws() => new()
    {
        new() { LawNumber = "قانون 131 لسنة 1948", Title = "القانون المدني", Category = LawCategory.Civil, Year = 1948 },
        new() { LawNumber = "قانون 58 لسنة 1937", Title = "قانون العقوبات", Category = LawCategory.Criminal, Year = 1937 },
        new() { LawNumber = "قانون 17 لسنة 1999", Title = "قانون الإجراءات الجنائية", Category = LawCategory.Criminal, Year = 1999 },
        new() { LawNumber = "قانون 13 لسنة 1968", Title = "قانون الإجراءات المدنية والتجارية", Category = LawCategory.Civil, Year = 1968 },
        new() { LawNumber = "قانون 17 لسنة 1981", Title = "قانون الطفل", Category = LawCategory.Family, Year = 1981 },
        new() { LawNumber = "قانون 1 لسنة 2000", Title = "قانون تنظيم بعض أوضاع وإجراءات التقاضي في مسائل الأحوال الشخصية", Category = LawCategory.Family, Year = 2000 },
        new() { LawNumber = "قانون 12 لسنة 2003", Title = "قانون العمل", Category = LawCategory.Labor, Year = 2003 },
        new() { LawNumber = "قانون 159 لسنة 1981", Title = "قانون شركات المساهمة", Category = LawCategory.Commercial, Year = 1981 },
    };
        
        // العلاقات
        public ICollection<LawArticle> Articles { get; set; } = new List<LawArticle>();
        public ICollection<LawAmendment> Amendments { get; set; } = new List<LawAmendment>();
        public ICollection<LawInterpretation> Interpretations { get; set; } = new List<LawInterpretation>();
        public ICollection<LawKeyword> Keywords { get; set; } = new List<LawKeyword>();
    }
}
  