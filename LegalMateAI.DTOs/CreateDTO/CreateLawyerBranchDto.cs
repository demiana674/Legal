// LegalMateAI.DTOs/CreateDTO/CreateLawyerBranchDto.cs
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateLawyerBranchDto
    {
        [Required(ErrorMessage = "اسم الفرع مطلوب")]
        [StringLength(100)]
        public string BranchName { get; set; } = string.Empty;
        
        public int? GovernorateId { get; set; }
        
        [StringLength(50)]
        public string? City { get; set; }
        
        [Required(ErrorMessage = "العنوان مطلوب")]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        public string? PhoneNumber { get; set; }
    }
}