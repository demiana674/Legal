using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateLawyerProfileDto
    {
        // البيانات الأساسية
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AlternativePhone { get; set; }
        public string? NationalId { get; set; }
        
        // البيانات المهنية
        public string? LicenseNumber { get; set; }
        public string? BarAssociation { get; set; }
        public int? YearsOfExperience { get; set; }
        
        // تخصصات المحامي (قائمة IDs)
        public List<int>? SpecialtyIds { get; set; }
        
        // الموقع
        public int? GovernorateId { get; set; }
        public string? City { get; set; }
        public string? OfficeAddress { get; set; }
    }
}