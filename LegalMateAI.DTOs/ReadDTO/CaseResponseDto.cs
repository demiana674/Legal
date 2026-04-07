// LegalMateAI.DTOs/ReadDTO/CaseResponseDto.cs
using System;
using System.Collections.Generic;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class CaseResponseDto
    {
        public Guid Id { get; set; }
        public string CaseNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // معلومات العميل
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string? ClientEmail { get; set; }
        
        // معلومات المحامي
        public Guid? LawyerId { get; set; }
        public string? LawyerName { get; set; }
        
        // معلومات القضية
        public string? Court { get; set; }
        public DateTime? NextHearingDate { get; set; }
        public string NextHearingDateFormatted => NextHearingDate?.ToString("dd MMM yyyy") ?? "غير محدد";
        
        public CaseStatus Status { get; set; }
        public string StatusName => Status switch
        {
            CaseStatus.Active => "نشطة",
            CaseStatus.Pending => "قيد المراجعة",
            CaseStatus.Completed => "منتهية",
            CaseStatus.Rejected => "مرفوضة",
            CaseStatus.OnHold => "معلقة",
            _ => Status.ToString()
        };
        
        public string StatusColor => Status switch
        {
            CaseStatus.Active => "#3DD68C",
            CaseStatus.Pending => "#F5A623",
            CaseStatus.Completed => "#4E9FE8",
            CaseStatus.Rejected => "#F2605A",
            CaseStatus.OnHold => "#9E9E9E",
            _ => "#9E9E9E"
        };
        
        public CasePriority Priority { get; set; }
        public string PriorityName => Priority switch
        {
            CasePriority.Low => "منخفضة",
            CasePriority.Medium => "متوسطة",
            CasePriority.High => "عالية",
            CasePriority.Urgent => "عاجلة",
            _ => Priority.ToString()
        };
        
        public string? CaseType { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted => CreatedAt.ToString("dd MMM yyyy");
        
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        
        // الإحصائيات
        public int DocumentsCount { get; set; }
        public int NotesCount { get; set; }
        
        // العلاقات
        public List<CaseDocumentResponseDto> Documents { get; set; } = new();
        public List<CaseNoteResponseDto> Notes { get; set; } = new();
    }
}