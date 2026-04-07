using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Enums
{
    public enum CourtLevel
{
    Primary = 1,        // ابتدائي
    Appeal = 2,         // استئناف
    Cassation = 3,      // نقض
    Constitutional = 4, // دستورية
    Administrative = 5, // إدارية
    Specialized = 6     // متخصصة
}
}