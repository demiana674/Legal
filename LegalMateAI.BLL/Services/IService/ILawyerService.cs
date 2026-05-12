// LegalMateAI.BLL/Services/IService/ILawyerService.cs
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.IService
{
    public interface ILawyerService
    {
        /// <summary>
        /// ✅ البحث عن محامين - يظهر فقط المحامين الـ Active
        /// </summary>
        Task<List<LawyerResponseDto>> SearchLawyersAsync(LawyerSearchDto searchCriteria);
        
        /// <summary>
        /// ✅ جلب محامي محدد - فقط لو حالته Active
        /// </summary>
        Task<LawyerResponseDto?> GetLawyerByIdAsync(Guid lawyerId);
        
        Task<List<AvailabilityDto>> GetLawyerAvailabilityAsync(Guid lawyerId);
        Task<bool> UpdateAvailabilityAsync(Guid lawyerId, List<CreateLawyerAvailabilityDto> availabilities);
        Task<List<LawyerSpecialtyResponseDto>> GetSpecialtiesAsync();
        Task<List<ReviewDto>> GetLawyerReviewsAsync(Guid lawyerId);
        Task<bool> AddReviewAsync(Guid userId, Guid lawyerId, int rating, string? comment, Guid? appointmentId);
        
        /// <summary>
        /// ✅ جلب محامين حسب التخصص - يظهر فقط الـ Active
        /// </summary>
        Task<List<LawyerResponseDto>> GetLawyersBySpecializationAsync(string specialization, int limit = 5);
    }
}