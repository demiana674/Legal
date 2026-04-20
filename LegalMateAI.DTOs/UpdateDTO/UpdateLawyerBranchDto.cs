// LegalMateAI.DTOs/UpdateDTO/UpdateLawyerBranchDto.cs
namespace LegalMateAI.DTOs.UpdateDTO
{
    /// <summary>
    /// نموذج تحديث فرع محامي
    /// </summary>
    public class UpdateLawyerBranchDto
    {
        /// <summary>
        /// اسم الفرع
        /// </summary>
        public string? BranchName { get; set; }
        
        /// <summary>
        /// معرف المحافظة
        /// </summary>
        public int? GovernorateId { get; set; }
        
        /// <summary>
        /// المدينة
        /// </summary>
        public string? City { get; set; }
        
        /// <summary>
        /// العنوان التفصيلي
        /// </summary>
        public string? Address { get; set; }
        
        /// <summary>
        /// رقم الهاتف
        /// </summary>
        public string? PhoneNumber { get; set; }
        
        /// <summary>
        /// هل الفرع نشط؟
        /// </summary>
        public bool? IsActive { get; set; }
    }
}