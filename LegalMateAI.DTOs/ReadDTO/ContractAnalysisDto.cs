namespace LegalMateAI.DTOs.ReadDTO
{
    public class PlaceholderFieldDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public string Label { get; set; } = string.Empty;
        public string? DefaultValue { get; set; }
        public bool IsRequired { get; set; } = true;
        public int Order { get; set; }
        public string? RegexPattern { get; set; }
        public string? Options { get; set; }
        public string? Placeholder { get; set; }
    }

    public class UniqueFieldDto
    {
        public string FieldId { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> TargetFields { get; set; } = new();
        public string? RegexPattern { get; set; }
        public string? Placeholder { get; set; }
        public bool IsRequired { get; set; } = true;
    }

    public class TemplateAnalysisDto
    {
        public Guid TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public int PlaceholdersCount { get; set; }
        public List<PlaceholderFieldDto> Placeholders { get; set; } = new();
        public List<UniqueFieldDto> UniqueFields { get; set; } = new();
        public List<string> SignatureFields { get; set; } = new();
        public string? Message { get; set; }
    }
}