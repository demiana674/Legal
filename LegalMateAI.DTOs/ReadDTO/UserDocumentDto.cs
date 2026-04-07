using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.ReadDTO
{
    // 3. مستند المستخدم
    public class UserDocumentDto
    {
        public Guid Id { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string DocumentUrl { get; set; } = string.Empty;
        public UserDocumentType DocumentType { get; set; }
        public string DocumentTypeName => DocumentType switch
        {
            UserDocumentType.NationalId => "بطاقة شخصية",
            UserDocumentType.Passport => "جواز سفر",
            UserDocumentType.BirthCertificate => "شهادة ميلاد",
            UserDocumentType.DriverLicense => "رخصة قيادة",
            UserDocumentType.Education => "مؤهل دراسي",
            _ => "مستند آخر"
        };
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedAtFormatted => UploadedAt.ToString("dd MMM yyyy");
        public bool IsVerified { get; set; }
    }
}



