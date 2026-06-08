using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class AddClientRequest
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [MaxLength(200, ErrorMessage = "الاسم لا يزيد عن 200 حرف")]
        public string ClientName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "رقم هاتف غير صحيح")]
        [MaxLength(20, ErrorMessage = "رقم الهاتف لا يزيد عن 20 رقم")]
        public string Phone { get; set; } = string.Empty;
        
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        [MaxLength(255, ErrorMessage = "البريد الإلكتروني لا يزيد عن 255 حرف")]
        public string? Email { get; set; }
        
        [Required(ErrorMessage = "عنوان القضية مطلوب")]
        [MaxLength(200, ErrorMessage = "عنوان القضية لا يزيد عن 200 حرف")]
        public string CaseTitle { get; set; } = string.Empty;
        
        [MaxLength(2000, ErrorMessage = "وصف القضية لا يزيد عن 2000 حرف")]
        public string? CaseDescription { get; set; }
        
        [MaxLength(200, ErrorMessage = "اسم المحكمة لا يزيد عن 200 حرف")]
        public string? Court { get; set; }
        
        [MaxLength(100, ErrorMessage = "نوع القضية لا يزيد عن 100 حرف")]
        public string? CaseType { get; set; }
        
        public bool IsUrgent { get; set; } = false;
    }
}