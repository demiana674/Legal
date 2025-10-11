using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Entities
{
    public class UserContracts
    {
        [Key]
        public int UserContractID { get; set; }
        [Required]
        public int UserID { get; set; }
        [Required]
        public int TemplateID { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime FilledDate { get; set; } = DateTime.UtcNow;
        public string? FilledData { get; set; }

        [Url]
        [MaxLength(500)]
        public string? PdfUrl { get; set; }
        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        public User User { get; set; }= null!; // Navigation property for User
        public ContractsTemplate Template { get; set; }= null!; // Navigation property for ContractsTemplate
    }
    public enum ContractStatus
    {
        Draft,        // المستخدم لسه بيعدل فيه
        Completed,    // جاهزالتحميل
        Signed        //  نهائي
    }
}