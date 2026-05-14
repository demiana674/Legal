using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalMateAI.Domain.Entities
{
    [Table("TimeDimensions")]
    public class TimeDimension
    {
        [Key]
        public int Id { get; set; }
        public int Year { get; set; }
        public int Quarter { get; set; }
        public int Month { get; set; }
        public int Week { get; set; }
        public int Day { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public bool IsWeekend { get; set; }
        public string Season { get; set; } = string.Empty;
    }
}