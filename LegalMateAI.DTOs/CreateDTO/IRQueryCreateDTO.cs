using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class IRQueryCreateDTO
    {
        [Required]
        public int UserID { get; set; }
        [Required]
        [MaxLength(2000)]
        public string QueryText { get; set; }= string.Empty;

    }
}