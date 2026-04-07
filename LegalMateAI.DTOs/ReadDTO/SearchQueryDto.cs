namespace LegalMateAI.DTOs.ReadDTO
{
    public class SearchQueryDto
    {
        public Guid Id { get; set; }
        public string Query { get; set; } = string.Empty;
        public string? ProcessedIntent { get; set; }
        public int ResultCount { get; set; }
        public DateTime SearchedAt { get; set; }
    }
}