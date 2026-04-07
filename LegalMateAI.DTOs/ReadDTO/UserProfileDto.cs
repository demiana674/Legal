namespace LegalMateAI.DTOs.ReadDTO
{
    public class UserProfileDto
    {
        // تم إضافة set لتجنب خطأ Read Only (CS0200)
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        // الخصائص التالية كانت مفقودة وتسبب خطأ CS0117
        public string? NationalId { get; set; }
        public string? Phone { get; set; }
        public string? AlternativePhone { get; set; }
        public string? GovernorateName { get; set; }
        public string? CityName { get; set; }
        public string? Address { get; set; }
        
        public string? ProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}