using LegalMateAI.DTOs;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IRegistrationService
    {
        /// <summary>
        /// تسجيل مستخدم جديد (يدعم جميع الأدوار)
        /// </summary>
        Task<RegistrationResult> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// تسجيل مستخدم عادي فقط (للمحافظة على التوافقية)
        /// </summary>
        Task<RegistrationResult> RegisterUserAsync(RegisterRequest request);
    }

    public class RegistrationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public Guid? UserId { get; set; }
        public bool RequiresApproval { get; set; }
    }
}