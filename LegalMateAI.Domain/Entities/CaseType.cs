using LegalMateAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegalMateAI.Domain.Enums;
namespace LegalMateAI.Domain.Entities
{

public class CaseType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int? SpecializationId { get; set; }
    public LegalSpecialization? Specialization { get; set; }
    
    public static List<CaseType> EgyptianCaseTypes() => new()
    {
        // مدني
        new() { Id = 1, Name = "Eviction Lawsuit", NameAr = "دعوى طرد", SpecializationId = 1 },
        new() { Id = 2, Name = "Debt Collection", NameAr = "مطالبة بدين", SpecializationId = 1 },
        new() { Id = 3, Name = "Compensation", NameAr = "تعويضات", SpecializationId = 1 },
        
        // أسرة
        new() { Id = 4, Name = "Divorce", NameAr = "دعوى طلاق", SpecializationId = 3 },
        new() { Id = 5, Name = "Custody", NameAr = "دعوى حضانة", SpecializationId = 3 },
        new() { Id = 6, Name = "Alimony", NameAr = "دعوى نفقة", SpecializationId = 3 },
        
        // جنائي
        new() { Id = 7, Name = "Theft", NameAr = "سرقة", SpecializationId = 2 },
        new() { Id = 8, Name = "Assault", NameAr = "ضرب", SpecializationId = 2 },
        new() { Id = 9, Name = "Fraud", NameAr = "نصب", SpecializationId = 2 },
        
        // عمل
        new() { Id = 10, Name = "Unfair Dismissal", NameAr = "فصل تعسفي", SpecializationId = 5 },
        new() { Id = 11, Name = "Wage Dispute", NameAr = "نزاع على أجور", SpecializationId = 5 },
        
        // تجاري
        new() { Id = 12, Name = "Commercial Contract Dispute", NameAr = "نزاع عقد تجاري", SpecializationId = 4 },
        new() { Id = 13, Name = "Bankruptcy", NameAr = "إفلاس", SpecializationId = 4 },
    };
}

}