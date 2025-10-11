using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class ContractsTemplateCreateDTO
    {

        [Required(ErrorMessage = "العنوان مطلوب")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "نوع العقد مطلوب")]
        [MaxLength(100)]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "محتوى العقد مطلوب")]
        public string Content { get; set; } = string.Empty;

    }
}