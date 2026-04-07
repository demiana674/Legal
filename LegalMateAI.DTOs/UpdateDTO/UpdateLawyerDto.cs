using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateLawyerDto
    {
        // البيانات الأساسية
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? NationalId { get; set; }
        
        // البيانات المهنية
        public string? LicenseNumber { get; set; }
        public string? BarAssociation { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Specialization { get; set; }
        
        // الموقع
        public int? GovernorateId { get; set; }
        public string? City { get; set; }
        public string? OfficeAddress { get; set; }
        
        // الصورة
        public string? ProfilePicture { get; set; }
        
        // الحالة
        public bool? IsActive { get; set; }
        public string? VerificationStatus { get; set; } // Pending, Approved, Rejected
        public string? RejectionReason { get; set; }
    }
}