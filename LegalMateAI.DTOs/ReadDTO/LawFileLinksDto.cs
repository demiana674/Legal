namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawFileLinksDto
    {
        public string? PdfFileUrl { get; set; }
        public string? SourceUrl { get; set; }
        public bool HasPdfLink => !string.IsNullOrEmpty(PdfFileUrl);
        public bool HasSourceLink => !string.IsNullOrEmpty(SourceUrl);
    }
}