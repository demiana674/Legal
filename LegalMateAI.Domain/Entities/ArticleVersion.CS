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
    // 6. الإصدارات السابقة للمواد (للحفاظ على تاريخ التعديلات)
    public class ArticleVersion
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }
        public LawArticle Article { get; set; } = null!;
        public string Content { get; set; } = string.Empty;
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? ChangeReason { get; set; }
        public Guid? ModifiedByAdminId { get; set; }
    }
}
