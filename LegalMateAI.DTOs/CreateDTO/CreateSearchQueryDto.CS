using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateSearchQueryDto
    {
        [Required(ErrorMessage = "استعلام البحث مطلوب")]
        [StringLength(500)]
        public string Query { get; set; } = string.Empty;

        public string[]? Filters { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}