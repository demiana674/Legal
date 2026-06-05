// LegalMateAI.DTOs/CreateDTO/LawyerSearchDto.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class LawyerSearchDto
    {
        public string? Specialization { get; set; }
        public string? Location { get; set; }
        public double? MinRating { get; set; }
        public decimal? MaxHourlyRate { get; set; }
        public int? MinExperience { get; set; }
        public string[]? Languages { get; set; }
        public string[]? Courts { get; set; }
        public bool OnlyAvailable { get; set; }
        public DateTime? PreferredDate { get; set; }
        public int? GovernorateId { get; set; }
        public int? SpecializationId { get; set; }  // ✅ تغيير من Guid? إلى int?
        public string? Governorate { get; set; }
        public string? City { get; set; }
        public int[]? AuthorizedCourtIds { get; set; }
    }
}