using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.CreateDTO
{
    // 14. Admin Verification
    public class VerifyLawyerDto
    {
        [Required(ErrorMessage = "معرف المحامي مطلوب")]
        public Guid LawyerId { get; set; }
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
        public string? AdminNotes { get; set; }
    }
}