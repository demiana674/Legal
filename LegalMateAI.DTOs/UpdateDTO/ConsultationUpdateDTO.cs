using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Entities;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class ConsultationUpdateDTO
    {

        
        public string? Answer { get; set; }
        public ConsultationStatus? Status { get; set; }
    }


}