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
    public class LegalSpecialization
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // التخصصات المصرية
    public static List<LegalSpecialization> EgyptianSpecializations() => new()
    {
        new() { Id = 1, Name = "Civil Law", NameAr = "القانون المدني" },
        new() { Id = 2, Name = "Criminal Law", NameAr = "القانون الجنائي" },
        new() { Id = 3, Name = "Family Law", NameAr = "قانون الأحوال الشخصية" },
        new() { Id = 4, Name = "Commercial Law", NameAr = "القانون التجاري" },
        new() { Id = 5, Name = "Labor Law", NameAr = "قانون العمل" },
        new() { Id = 6, Name = "Administrative Law", NameAr = "القانون الإداري" },
        new() { Id = 7, Name = "Real Estate Law", NameAr = "القانون العقاري" },
        new() { Id = 8, Name = "Tax Law", NameAr = "قانون الضرائب" },
        new() { Id = 9, Name = "Constitutional Law", NameAr = "القانون الدستوري" },
        new() { Id = 10, Name = "International Law", NameAr = "القانون الدولي" },
    };
}

}

