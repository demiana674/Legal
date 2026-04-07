using LegalMateAI.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateContractDto
    {
        [Required(ErrorMessage = "عنوان العقد مطلوب")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "نوع العقد مطلوب")]
        public ContractType Type { get; set; }

        [Required(ErrorMessage = "اسم الطرف الثاني مطلوب")]
        [StringLength(200)]
        public string PartyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ بدء العقد مطلوب")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public string? Value { get; set; }

        public decimal? MonetaryValue { get; set; }

        public Guid? LawyerId { get; set; }

        public Guid? TemplateId { get; set; }

        public Dictionary<string, string>? CustomFields { get; set; }
    }
}