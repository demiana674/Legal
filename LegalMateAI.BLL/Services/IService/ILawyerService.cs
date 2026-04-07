// LegalMateAI.BLL/Services/IService/ILawyerService.cs
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.IService
{
    public interface ILawyerService
    {
        Task<List<LawyerResponseDto>> SearchLawyersAsync(LawyerSearchDto searchCriteria);
        Task<LawyerResponseDto?> GetLawyerByIdAsync(Guid lawyerId);
        Task<List<AvailabilityDto>> GetLawyerAvailabilityAsync(Guid lawyerId);
        Task<bool> UpdateAvailabilityAsync(Guid lawyerId, List<CreateLawyerAvailabilityDto> availabilities);
        
        // ✅ جلب تخصصات المحامي
        Task<List<LawyerSpecialtyResponseDto>> GetSpecialtiesAsync();
        
        Task<List<ReviewDto>> GetLawyerReviewsAsync(Guid lawyerId);
        Task<bool> AddReviewAsync(Guid userId, Guid lawyerId, int rating, string? comment, Guid? appointmentId);
        Task<List<LawyerResponseDto>> GetLawyersBySpecializationAsync(string specialization, int limit = 5);
    }
}