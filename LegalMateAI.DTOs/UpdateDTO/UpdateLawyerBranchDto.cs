namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateLawyerBranchDto
    {
        public string? BranchName { get; set; }
        public int? GovernorateId { get; set; }
        public int? CityId { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? IsActive { get; set; }
    }
}