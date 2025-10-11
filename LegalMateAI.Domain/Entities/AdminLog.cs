using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Entities
{
    [Index(nameof(AdminID))]
    [Index(nameof(Timestamp))]
    public class AdminLog
    {
        [Key]
      public int LogID { get; set; }
        [Required]
        public int AdminID { get; set; }
        [Required]
        public AdminActionType ActionType { get; set; } = AdminActionType.Other;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        [MaxLength(1000)]
        public string? Details { get; set; }
        [MaxLength(100)]
        public string? EntityName { get; set; }
        public int? EntityID { get; set; }

        public Admin Admin { get; set; } = null!;
    }
    public enum AdminActionType
    {
        Add,
        Update,
        Delete,
        Login,
        Logout,
        Approve,
        Reject,
        View,
        Other
    }
}