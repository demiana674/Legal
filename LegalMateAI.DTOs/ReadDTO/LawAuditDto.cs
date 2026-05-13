using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawAuditDto
    {
        public string AddedByAdminName { get; set; } = string.Empty;
        public string? UploadedByUserName { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}