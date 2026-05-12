using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IAppointmentService
    {
        // ========== الحجز والموافقة ==========
        Task<AppointmentResponseDto?> CreateAppointmentAsync(Guid userId, CreateAppointmentDto request);
        Task<bool> ApproveAppointmentAsync(Guid lawyerId, Guid appointmentId);
        Task<bool> RejectAppointmentAsync(Guid lawyerId, Guid appointmentId, string? reason);
        
        // ========== عرض المواعيد ==========
        Task<List<AppointmentResponseDto>> GetUserAppointmentsAsync(Guid userId, string? status = null);
        Task<List<AppointmentResponseDto>> GetLawyerAppointmentsAsync(Guid lawyerId, string? status = null);
        Task<AppointmentResponseDto?> GetAppointmentByIdAsync(Guid appointmentId);
        
        // ========== إلغاء ==========
        Task<bool> CancelAppointmentAsync(Guid appointmentId, Guid userId, string? reason = null);
        
        // ========== إعادة الجدولة ==========
        Task<RescheduleResponseDto?> RequestRescheduleAsync(Guid userId, CreateRescheduleRequestDto request, bool isLawyer = false);
        Task<bool> RespondToRescheduleAsync(Guid userId, Guid rescheduleId, UpdateRescheduleRequestDto request, bool isLawyer = false);
        Task<List<RescheduleResponseDto>> GetRescheduleRequestsForLawyerAsync(Guid lawyerId);
        Task<List<RescheduleResponseDto>> GetRescheduleRequestsForUserAsync(Guid userId);
    }
}