 using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Enums
{
 
 public enum UserDocumentType
    {
        NationalId = 1,      // بطاقة شخصية
        Passport = 2,        // جواز سفر
        BirthCertificate = 3, // شهادة ميلاد
        DriverLicense = 4,    // رخصة قيادة
        Education = 5,        // مؤهل دراسي
        Other = 6
    }
}