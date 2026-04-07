namespace LegalMateAI.DTOs.ReadDTO
{
    public class SmartSearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}