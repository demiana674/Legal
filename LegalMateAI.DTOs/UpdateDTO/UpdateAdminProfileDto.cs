using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateAdminProfileDto
    {
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        public string? PhoneNumber { get; set; }

        [Phone(ErrorMessage = "رقم الهاتف البديل غير صحيح")]
        public string? AlternativePhone { get; set; }

        public string? Address { get; set; }

        public string? Governorate { get; set; }

        public string? City { get; set; }
    }
}