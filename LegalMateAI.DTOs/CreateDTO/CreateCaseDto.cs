using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateCaseDto
    {
        // بيانات القضية
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Court { get; set; }
        public DateTime? NextHearingDate { get; set; }
        public CaseStatus Status { get; set; } = CaseStatus.Pending;
        public CasePriority Priority { get; set; } = CasePriority.Medium;
        public string? CaseType { get; set; }
        
        // ✅ Client ID (إذا كان موجوداً مسبقاً)
        public Guid? ClientId { get; set; }
        
        // ✅ بيانات الموكل الجديد (إذا لم يكن موجوداً)
        public string? ClientFirstName { get; set; }
        public string? ClientLastName { get; set; }
        public string? ClientEmail { get; set; }
        public string? ClientPhone { get; set; }
        public string? ClientNationality { get; set; }
        public DateTime? ClientDateOfBirth { get; set; }
        public string? ClientGovernorate { get; set; }
        public string? ClientCity { get; set; }
        public string? ClientAddress { get; set; }
    }
}