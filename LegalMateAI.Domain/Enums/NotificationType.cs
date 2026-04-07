using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Enums
{
    public enum NotificationType
    {
        Info = 1,
        Success = 2,
        Warning = 3,
        Error = 4
    }
}