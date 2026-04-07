using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Enums;
using LegalMateAI.Domain.Entities;
namespace LegalMateAI.Domain.Entities
{
public class LawyerReview
{
    public Guid Id { get; set; }
    public Guid LawyerId { get; set; }
    public Guid UserId { get; set; }
    public Guid? AppointmentId { get; set; } // للتأكد من أن المراجع حجز فعلاً
    
    [Range(1, 5)]
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsVerified { get; set; } // تأكيد أن التقييم حقيقي
    
    public LawyerProfile Lawyer { get; set; } = null!;
    public User User { get; set; } = null!;
    public Appointment? Appointment { get; set; }
}


}