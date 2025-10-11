namespace LegalMateAI.DTOs.ReadDTO
{
    public class ArticleReadDTO
    {
        public int ArticleID { get; set; }
        public int LawID { get; set; }
        public string Text { get; set; } = string.Empty;
        public string ArticleNumber { get; set; }= string.Empty;
        public string? Notes { get; set; }
        public string? LawTitle { get; set; }


    }
}
