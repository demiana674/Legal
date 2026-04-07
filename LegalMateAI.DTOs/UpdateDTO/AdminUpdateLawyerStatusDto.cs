using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
namespace LegalMateAI.DTOs.UpdateDTO
{
    // 16. Admin Update Lawyer Status
    public class AdminUpdateLawyerStatusDto
    {
    [Required(ErrorMessage = "معرف المحامي مطلوب")]
        public Guid LawyerId { get; set; }  // ✅ ده مهم عشان تعرف مين المحامي

        [Required(ErrorMessage = "الحالة مطلوبة")]
         public LawyerVerificationStatus Status { get; set; }  // ✅ من Enums

        public string? Notes { get; set; }  // ✅ ملاحظات (اختياري)
    }


    }
