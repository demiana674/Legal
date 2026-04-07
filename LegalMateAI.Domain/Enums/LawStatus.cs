using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Enums
{  
  
  // 2. حالة القانون
    public enum LawStatus
    {
        Active = 1,           // ساري
        Amended = 2,          // معدل
        Repealed = 3,         // ملغي
        Draft = 4,            // مسودة
        UnderReview = 5       // قيد المراجعة
    }

}