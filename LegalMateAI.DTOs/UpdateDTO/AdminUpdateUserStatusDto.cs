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
    // 17. Admin Update User Status

using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class AdminUpdateUserStatusDto
    {
        [Required(ErrorMessage = "معرف المستخدم مطلوب")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "الحالة مطلوبة")]
        public AccountStatus Status { get; set; }

        public string? Reason { get; set; }
    }
}
}