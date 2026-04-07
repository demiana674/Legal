using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// LegalMateAI.Domain/Enums/LawyerVerificationStatus.cs
namespace LegalMateAI.Domain.Enums
{
    public enum LawyerVerificationStatus
    {
        Pending = 1,
        Active = 2,
        Locked = 3,
        Suspended = 4,
        Deactivated = 5
    }
}