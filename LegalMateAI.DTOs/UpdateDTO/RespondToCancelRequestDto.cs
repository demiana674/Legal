namespace LegalMateAI.DTOs.UpdateDTO
{
    public class RespondToCancelRequestDto
    {
        public bool Approve { get; set; }
        public string? Reason { get; set; }
    }
}