using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateDocumentAnalysisDto
    {
        [Required(ErrorMessage = "معرف المستند مطلوب")]
        // public Guid DocumentId { get; set; }

        public bool ExtractClauses { get; set; } = true;

        public bool AssessRisks { get; set; } = true;

        public bool SuggestLawyers { get; set; } = true;
    }
}