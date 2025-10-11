using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Entities;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class SearchIndexCreateDTO
    {
        
        public int EntityId { get; set; }
        
        public string? Keywords { get; set; }
        public EntityType EntityType { get; set; } = EntityType.Other;

     
        public string? Content { get; set; }

    }
}
