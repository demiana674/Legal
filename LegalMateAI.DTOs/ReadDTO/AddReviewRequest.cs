namespace LegalMateAI.DTOs.ReadDTO
{
    public class AddReviewRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public Guid? AppointmentId { get; set; }
    }
}