using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateAdminProfileDto
    {
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        public string? PhoneNumber { get; set; }
    }
}