 using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Enums
{
 
 // 9. مصدر التفسير
    public enum InterpretationSource
    {
        CourtOfCassation = 1,     // محكمة النقض
        ConstitutionalCourt = 2,   // المحكمة الدستورية
        LegalScholars = 3,         // فقهاء القانون
        AcademicResearch = 4,      // أبحاث أكاديمية
        AdminAdded = 5             // إضافة من الأدمن
    }

}








