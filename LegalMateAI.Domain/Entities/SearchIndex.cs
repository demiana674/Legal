using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LegalMateAI.Domain.Entities
{
    [Index(nameof(EntityType))]
    [Index(nameof(EntityId))]
    [Index(nameof(Keywords))]
    [Index(nameof(IndexedAt))]
    public class SearchIndex
    {
        [Key]
        public int IndexId { get; set; }
        [Required]
        public int EntityId { get; set; }
        [Required]
        public EntityType EntityType { get; set; } = EntityType.Other;
        public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
        [MaxLength(1000)]
        public string? Keywords { get; set; }
        [MaxLength(4000)]
        public string? Content { get; set; }
        public byte[]? EmbeddingVector { get; set; }
    }
    public enum EntityType
    {
        Law,
        Article,
        Clause,
        Contract,
        Document,
        Query,
        User,
        Notification,
        Other
    }
}