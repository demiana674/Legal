// LegalMateAI.DTOs/ReadDTO/CaseNoteResponseDto.cs
using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class CaseNoteResponseDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string WrittenByName { get; set; } = string.Empty;
        public string? WrittenByRole { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted => CreatedAt.ToString("dd MMM yyyy HH:mm");
        public DateTime? UpdatedAt { get; set; }
        public bool IsPrivate { get; set; }
    }
}