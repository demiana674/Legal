using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalMateAI.Domain.Entities
{
    [Table("LocationDimensions")]
    public class LocationDimension
    {
        [Key]
        public int Id { get; set; }
        public int GovernorateId { get; set; }
        public string GovernorateName { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public bool IsUrban { get; set; }
    }
}