using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Entities;


namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawUpdateReadDTO
    {
        public int UpdateID { get; set; }
        public int LawID { get; set; }
        public string OldText { get; set; } = string.Empty;
        public string NewText { get; set; } = string.Empty;
        public string? UpdateSource { get; set; }
        public string? Summary { get; set; }
        public DateTime UpdateDate { get; set; }
        public LawChangeType ChangeType { get; set; } = LawChangeType.Amendment;
        public string? LawTitle { get; set; }

    }

}