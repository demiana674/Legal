using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Enums
{
    public enum LogAction
    {
        Create = 1,
        Read = 2,
        Update = 3,
        Delete = 4,
        Verify = 5,
        Reject = 6,
        Login = 7,
        Logout = 8,
        Export = 9
    }
}