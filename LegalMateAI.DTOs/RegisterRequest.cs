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

        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "الرقم القومي مطلوب")]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "الرقم القومي يجب أن يكون 14 رقم")]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "الرقم القومي يجب أن يتكون من 14 رقم فقط")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "نوع المستخدم مطلوب")]
        public UserRole Role { get; set; } = UserRole.User;

        // حقول المحامي فقط
        [RequiredIf("Role", UserRole.Lawyer, ErrorMessage = "رقم رخصة المحاماة مطلوب للمحامي")]
        [RegularExpression(@"^[A-Z]{2,3}-\d{4,8}$", 
            ErrorMessage = "صيغة رخصة المحاماة غير صحيحة. مثال: LAW-12345 أو BAR-67890")]
        public string? LicenseNumber { get; set; }

        [RequiredIf("Role", UserRole.Lawyer, ErrorMessage = "جهة النقابة مطلوبة للمحامي")]
        public string? BarAssociation { get; set; }

        [Range(0, 70, ErrorMessage = "سنوات الخبرة بين 0 و 70")]
        public int? YearsOfExperience { get; set; }
    }

    // ✅ Attribute مخصص للتحقق من الشرط
    public class RequiredIfAttribute : ValidationAttribute
    {
        private readonly string _propertyName;
        private readonly object _targetValue;

        public RequiredIfAttribute(string propertyName, object targetValue)
        {
            _propertyName = propertyName;
            _targetValue = targetValue;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var property = validationContext.ObjectType.GetProperty(_propertyName);
            if (property == null)
                return new ValidationResult($"الخاصية {_propertyName} غير موجودة");

            var propertyValue = property.GetValue(validationContext.ObjectInstance);

            if (propertyValue != null && propertyValue.Equals(_targetValue))
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return new ValidationResult(ErrorMessage ?? $"الحقل مطلوب");
                }
            }

            return ValidationResult.Success;
        }
    }
}