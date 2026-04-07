using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;

namespace LegalMateAI.BLL.Services.IService
{
    /// <summary>
    /// واجهة خدمة إدارة المواعيد
    /// </summary>
    public interface IAppointmentService
    {
        /// <summary>
        /// إنشاء موعد جديد
        /// </summary>
        Task<AppointmentResponseDto?> CreateAppointmentAsync(Guid userId, CreateAppointmentDto request);
        
        /// <summary>
        /// الحصول على مواعيد المستخدم
        /// </summary>
        Task<List<AppointmentResponseDto>> GetUserAppointmentsAsync(Guid userId, string? status = null);
        
        /// <summary>
        /// الحصول على مواعيد المحامي
        /// </summary>
        Task<List<AppointmentResponseDto>> GetLawyerAppointmentsAsync(Guid lawyerId, string? status = null);
        
        /// <summary>
        /// الحصول على موعد محدد
        /// </summary>
        Task<AppointmentResponseDto?> GetAppointmentByIdAsync(Guid appointmentId);
        
        /// <summary>
        /// إلغاء موعد
        /// </summary>
        Task<bool> CancelAppointmentAsync(Guid appointmentId, string? reason = null);
        
        /// <summary>
        /// طلب إعادة جدولة موعد
        /// </summary>
        Task<RescheduleResponseDto?> RequestRescheduleAsync(Guid userId, CreateRescheduleRequestDto request, bool isLawyer = false);
        
        /// <summary>
        /// الموافقة أو رفض طلب إعادة الجدولة
        /// </summary>
        Task<bool> RespondToRescheduleAsync(Guid userId, Guid rescheduleId, UpdateRescheduleRequestDto request, bool isLawyer = false);
        
        /// <summary>
        /// جلب طلبات إعادة الجدولة للمحامي
        /// </summary>
        Task<List<RescheduleResponseDto>> GetRescheduleRequestsForLawyerAsync(Guid lawyerId);
        
        /// <summary>
        /// جلب طلبات إعادة الجدولة للمستخدم
        /// </summary>
        Task<List<RescheduleResponseDto>> GetRescheduleRequestsForUserAsync(Guid userId);
    }
}