using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Enums
{
    public enum LawCategory
    {
        Constitutional = 1,    // دستوري
        Civil = 2,              // مدني
        Criminal = 3,           // جنائي
        Commercial = 4,         // تجاري
        Labor = 5,              // عمالي
        Tax = 6,                // ضريبي
        Family = 7,             // أسري
        Procedure = 8,          // إجرائي
        RealEstate = 9,         // عقاري
        Financial = 10,         // مالي ومصرفي
        Investment = 11,        // استثماري
        Social = 12,            // اجتماعي
        Educational = 13,       // تعليمي
        Economic = 14,          // اقتصادي
        Maritime = 15,          // بحري (قيمة جديدة)
        Administrative = 16,    // إداري
        International = 17,     // دولي
        Other = 99              // أخرى
    }
}