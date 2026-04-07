using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.UpdateDTO
{
   public class UpdateUserProfileDto
{

    
    // أرقام التواصل
    public string? PhoneNumber { get; set; }
    public string? AlternativePhone { get; set; }

    // العنوان
         public int? GovernorateId { get; set; }
        public string? GovernorateName { get; set; }
        public int? CityId {get; set;}
        public string? City { get; set; }
    public string? Address { get; set; }
    
    // إعدادات
    public string? Theme { get; set; }
    public bool? IsProfilePublic { get; set; }
}
}