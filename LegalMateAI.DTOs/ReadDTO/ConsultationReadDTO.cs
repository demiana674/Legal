using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ConsultationReadDTO
    {
        public int ConsultationID { get; set; }
        public int UserID { get; set; }
        public string? UserName { get; set; }
        public DateTime DateAsked { get; set; }
        public DateTime? DateAnswered { get; set; }
        public string? Question { get; set; }
        public string? Answer { get; set; }
      
        public int? LawyerID { get; set; }
        public string? LawyerName { get; set; }





        public ConsultationStatus? Status { get; set; }
    }
    public enum ConsultationStatus
    {
        Pending,    // لسه في انتظار الرد
        InProgress, // بيتم الرد حاليًا
        Answered,   // تم الرد
        Rejected,   // تم رفضها
        Closed      // انتهت رسميًا
    }
}