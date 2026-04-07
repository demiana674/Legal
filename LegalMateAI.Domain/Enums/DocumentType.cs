using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Enums
{
    public enum DocumentType
    {
        NationalId = 1,
        BirthCertificate = 2,
        Contract = 3,
        CourtDocument = 4,
        License = 5,
        Certificate = 6,
        PowerOfAttorney = 7,
        Other = 8
    }
}
