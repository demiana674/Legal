using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateContractDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        public string? Content { get; set; }

        [StringLength(200)]
        public string? PartyName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public string? Value { get; set; }

        public decimal? MonetaryValue { get; set; }

        [Range(0, 100)]
        public int? ProgressPercentage { get; set; }
    }
}