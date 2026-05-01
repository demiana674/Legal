// LegalMateAI.DTOs/RegisterRequest.cs
using System.ComponentModel.DataAnnotations;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "الاسم الأول بين 2 و 50 حرف")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الأخير مطلوب")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "الاسم الأخير بين 2 و 50 حرف")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(8, ErrorMessage = "كلمة المرور لا تقل عن 8 أحرف")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "كلمة المرور يجب أن تحتوي على: حرف كبير، حرف صغير، رقم، ورمز خاص (@$!%*?&)")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [Compare("Password", ErrorMessage = "كلمة المرور وتأكيدها غير متطابقين")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "الرقم القومي مطلوب")]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "الرقم القومي يجب أن يكون 14 رقم")]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "الرقم القومي يجب أن يتكون من 14 رقم فقط")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "الجنسية مطلوبة")]
        public string? Nationality { get; set; } 

        // ✅ معرف المحافظة - رقم مش اسم
        public int? GovernorateId { get; set; }
        
        // ✅ معرف المدينة - رقم مش اسم
        public int? CityId { get; set; }
        
        
        [StringLength(300)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "نوع المستخدم مطلوب")]
        // public string? Gender { get; set; }
        public UserRole Role { get; set; } = UserRole.User;

        // محامي فقط
        [RegularExpression(@"^[A-Z]{2,3}-\d{4,8}$", 
            ErrorMessage = "صيغة رخصة المحاماة غير صحيحة. مثال: LAW-12345")]
        public string? LicenseNumber { get; set; }

        public string? BarAssociation { get; set; }
        public DateTime? LicenseIssueDate { get; set; }
        public string? PracticeDegree { get; set; }

        [Range(0, 70, ErrorMessage = "سنوات الخبرة بين 0 و 70")]
        public int? YearsOfExperience { get; set; }
    }
}