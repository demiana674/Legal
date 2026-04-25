// LegalMateAI.DTOs/RegisterRequest.cs
using System.ComponentModel.DataAnnotations;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs
{
    public class RegisterRequest
    {
        // ========== البيانات الأساسية (للمستخدم والمحامي) ==========
        
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

        // ========== بيانات شخصية إضافية ==========
        
        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "الجنسية مطلوبة")]
        public string Nationality { get; set; } = "مصري";

        // ========== الموقع ==========
        
        public int? GovernorateId { get; set; }
        
        [StringLength(100)]
        public string? City { get; set; }
        
        [StringLength(300)]
        public string? Address { get; set; }

        // ========== نوع المستخدم ==========
        
        [Required(ErrorMessage = "نوع المستخدم مطلوب")]
        public UserRole Role { get; set; } = UserRole.User;

        // ========== حقول المحامي فقط ==========
        
        [RequiredIfRole(UserRole.Lawyer, ErrorMessage = "رقم رخصة المحاماة مطلوب للمحامي")]
        [RegularExpression(@"^[A-Z]{2,3}-\d{4,8}$", 
            ErrorMessage = "صيغة رخصة المحاماة غير صحيحة. مثال: LAW-12345 أو BAR-67890")]
        public string? LicenseNumber { get; set; }

        [RequiredIfRole(UserRole.Lawyer, ErrorMessage = "جهة النقابة مطلوبة للمحامي")]
        public string? BarAssociation { get; set; }

        [RequiredIfRole(UserRole.Lawyer, ErrorMessage = "تاريخ القيد مطلوب للمحامي")]
        public DateTime? LicenseIssueDate { get; set; }

        [RequiredIfRole(UserRole.Lawyer, ErrorMessage = "درجة المزاولة مطلوبة للمحامي")]
        public string? PracticeDegree { get; set; }

        [Range(0, 70, ErrorMessage = "سنوات الخبرة بين 0 و 70")]
        public int? YearsOfExperience { get; set; }
    }

    /// <summary>
    /// Attribute مخصص للتحقق من شرط الدور
    /// </summary>
    public class RequiredIfRoleAttribute : ValidationAttribute
    {
        private readonly UserRole _requiredRole;

        public RequiredIfRoleAttribute(UserRole requiredRole)
        {
            _requiredRole = requiredRole;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var roleProperty = validationContext.ObjectType.GetProperty("Role");
            if (roleProperty == null)
                return new ValidationResult($"خاصية Role غير موجودة");

            var roleValue = roleProperty.GetValue(validationContext.ObjectInstance);
            
            if (roleValue is UserRole role && role == _requiredRole)
            {
                if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                {
                    return new ValidationResult(ErrorMessage ?? "هذا الحقل مطلوب");
                }
            }

            return ValidationResult.Success;
        }
    }
}