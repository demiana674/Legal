// ContractTemplateDetailsDto.cs
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ContractTemplateDetailsDto : ContractTemplateResponseDto
    {
        public List<string> RequiredPlaceholders { get; set; } = new List<string>();
    }
}