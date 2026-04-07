namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateContractTemplateDto
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? TemplateContent { get; set; }

        public string[]? Placeholders { get; set; }

        public bool? IsActive { get; set; }
    }
}