namespace LegalMateAI.DTOs.ReadDTO
{
    public class LegalSpecializationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}