namespace LegalMateAI.DTOs.ReadDTO
{
    public class UserProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? NationalId { get; set; }
        public string? Phone { get; set; }
        public string? AlternativePhone { get; set; }
        public string? GovernorateName { get; set; }
        public string? CityName { get; set; }
        public string? Address { get; set; }
        public string? ProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        
        // بيانات شخصية إضافية
        public string? DateOfBirth { get; set; }
        // public string? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? LastPasswordChange { get; set; }
    }
}