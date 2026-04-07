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
   public class EgyptianCourt
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public CourtLevel Level { get; set; }
    public int? GovernorateId { get; set; }
    public Governorate? Governorate { get; set; }
    
    public static List<EgyptianCourt> EgyptianCourts() => new()
    {
        // محاكم ابتدائية
        new() { Id = 1, Name = "Cairo Primary Court", NameAr = "محكمة القاهرة الابتدائية", Level = CourtLevel.Primary, GovernorateId = 1 },
        new() { Id = 2, Name = "Giza Primary Court", NameAr = "محكمة الجيزة الابتدائية", Level = CourtLevel.Primary, GovernorateId = 2 },
        new() { Id = 3, Name = "Alexandria Primary Court", NameAr = "محكمة الإسكندرية الابتدائية", Level = CourtLevel.Primary, GovernorateId = 3 },
        
        // محاكم استئناف
        new() { Id = 4, Name = "Cairo Court of Appeal", NameAr = "محكمة استئناف القاهرة", Level = CourtLevel.Appeal, GovernorateId = 1 },
        new() { Id = 5, Name = "Alexandria Court of Appeal", NameAr = "محكمة استئناف الإسكندرية", Level = CourtLevel.Appeal, GovernorateId = 3 },
        
        // محاكم عليا
        new() { Id = 6, Name = "Court of Cassation", NameAr = "محكمة النقض", Level = CourtLevel.Cassation },
        new() { Id = 7, Name = "Supreme Constitutional Court", NameAr = "المحكمة الدستورية العليا", Level = CourtLevel.Constitutional },
        new() { Id = 8, Name = "State Council", NameAr = "مجلس الدولة", Level = CourtLevel.Administrative },
        
        // محاكم متخصصة
        new() { Id = 9, Name = "Family Court", NameAr = "محكمة الأسرة", Level = CourtLevel.Specialized, GovernorateId = 1 },
        new() { Id = 10, Name = "Economic Court", NameAr = "محكمة القضاء الاقتصادي", Level = CourtLevel.Specialized, GovernorateId = 1 },
    };
}

}
