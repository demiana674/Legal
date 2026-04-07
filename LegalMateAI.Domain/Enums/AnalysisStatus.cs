using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Enums
{
    public enum AnalysisStatus
    {
        Queued = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4
    }
}
