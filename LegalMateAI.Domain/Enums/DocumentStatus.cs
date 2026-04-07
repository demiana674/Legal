using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Enums
{

    public enum DocumentStatus
    {
        Pending = 1,
        Verified = 2,
        Rejected = 3,
        Expired = 4
    }
}
