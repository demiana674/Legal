// LegalMateAI.DTOs/ReadDTO/LawyerSkillResponseDto.cs
namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawyerSkillResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? Category { get; set; }
    }
}