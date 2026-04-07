using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
      public class GovernorateListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
