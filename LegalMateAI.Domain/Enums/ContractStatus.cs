using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Enums
{
    public enum ContractStatus
    {
        Draft = 1,
        PendingSignature = 2,
        Active = 3,
        Expired = 4,
        Terminated = 5
    }
}