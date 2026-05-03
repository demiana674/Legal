// LegalMateAI.DTOs/UpdateDTO/UpdateUserStatusDto.cs
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateUserStatusDto
    {
        /// <summary>
        /// الحالة الجديدة للمستخدم (Active, Suspended, Deactivated)
        /// </summary>
        public AccountStatus Status { get; set; }

        /// <summary>
        /// سبب التغيير (مطلوب في حالة التعليق أو الحظر)
        /// </summary>
        public string? Reason { get; set; }
    }
}