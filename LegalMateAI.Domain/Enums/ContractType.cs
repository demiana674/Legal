using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Enums
{
    public enum ContractType
    {
        Rental = 1,
        Employment = 2,
        Sale = 3,
        Service = 4,
        Partnership = 5,
        PowerOfAttorney = 6,
        Settlement = 7,
        Other = 8
    }
}