using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Entities;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class LawUpdateUpdateDTO
    {
        public string? OldText { get; set; }
        public string? NewText { get; set; }
        public string? UpdateSource { get; set; }
        public string? Summary { get; set; }
        public LawChangeType? ChangeType { get; set; }

    }

}