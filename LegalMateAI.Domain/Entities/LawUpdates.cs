using LegalMateAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LegalMateAI.Domain.Entities
{
    [Index(nameof(UpdateDate))]
    [Index(nameof(LawID))]
    public class LawUpdates
    {
        [Key]
        public int UpdateID { get; set; }
        [Required]
        public int LawID { get; set; }
        
        [Required]
        public string OldText { get; set; } = string.Empty;

        [Required]
        public string NewText { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? UpdateSource { get; set; }
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
        [MaxLength(500)]
        public string? Summary { get; set; }
        public LawChangeType ChangeType { get; set; } = LawChangeType.Amendment;
        public Law Law { get; set; }= null!;

    }
    public enum LawChangeType
    {
        Addition,
        Amendment,   
        Deletion,  
        Correction
    }
}