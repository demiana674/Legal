// LegalMateAI.DTOs/ReadDTO/AvailableTimeSlotDto.cs
namespace LegalMateAI.DTOs.ReadDTO
{
    public class AvailableTimeSlotDto
    {
        public string Time { get; set; } = string.Empty;
        public string DisplayTime { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }
}