namespace LegalMateAI.DTOs.ReadDTO
{
    public class VerifyLawyerRequest
    {
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
    }
}