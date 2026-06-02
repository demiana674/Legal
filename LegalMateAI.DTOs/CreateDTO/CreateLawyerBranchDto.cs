// LegalMateAI.DTOs/CreateDTO/CreateLawyerBranchDto.cs
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateLawyerBranchDto
    {
        [Required(ErrorMessage = "اسم الفرع مطلوب")]
        [StringLength(100)]
        public string BranchName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "المحافظة مطلوبة")]
        public int GovernorateId { get; set; }  // ✅ غير إلى int (غير nullable)
        
        [Required(ErrorMessage = "المدينة مطلوبة")]
        public int CityId { get; set; }  // ✅ غير إلى int (غير nullable)
        
        [Required(ErrorMessage = "العنوان مطلوب")]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        public string? PhoneNumber { get; set; }
    }
}