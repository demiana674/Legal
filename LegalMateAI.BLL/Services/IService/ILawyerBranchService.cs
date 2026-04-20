// LegalMateAI.BLL/Services/IService/ILawyerBranchService.cs
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.UpdateDTO;
namespace LegalMateAI.BLL.Services.IService
{
    public interface ILawyerBranchService
    {
        // ========== للجميع ==========
        
        /// <summary>
        /// جلب فروع محامي معين
        /// </summary>
        Task<List<LawyerBranchDto>> GetLawyerBranchesAsync(Guid lawyerId);
        
        /// <summary>
        /// جلب أوقات التوفر لفرع معين
        /// </summary>
        Task<List<BranchAvailabilityDto>> GetBranchAvailabilityAsync(Guid branchId);
        
        /// <summary>
        /// جلب المواعيد المتاحة لفرع في يوم معين
        /// </summary>
        Task<List<AvailableTimeSlotDto>> GetAvailableTimeSlotsAsync(Guid branchId, DateTime date);
        
        // ========== للمحامي فقط ==========
        
        /// <summary>
        /// إضافة فرع جديد
        /// </summary>
        Task<LawyerBranchDto?> CreateBranchAsync(Guid lawyerId, CreateLawyerBranchDto request);
        
        /// <summary>
        /// تحديث فرع
        /// </summary>
        Task<LawyerBranchDto?> UpdateBranchAsync(Guid lawyerId, Guid branchId, UpdateLawyerBranchDto request);
        
        /// <summary>
        /// حذف فرع
        /// </summary>
        Task<bool> DeleteBranchAsync(Guid lawyerId, Guid branchId);
        
        /// <summary>
        /// تحديث أوقات التوفر لفرع
        /// </summary>
        Task<bool> UpdateBranchAvailabilityAsync(Guid lawyerId, Guid branchId, List<CreateBranchAvailabilityDto> availabilities);
    }
}