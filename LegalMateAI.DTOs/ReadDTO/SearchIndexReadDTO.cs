using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Entities;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class SearchIndexReadDTO
    {
        public int IndexId { get; set; }
        public int EntityId { get; set; }
        public EntityType EntityType { get; set; }
        public string? Keywords { get; set; }
        public string? Content { get; set; }
        public DateTime IndexedAt { get; set; }

    }
}