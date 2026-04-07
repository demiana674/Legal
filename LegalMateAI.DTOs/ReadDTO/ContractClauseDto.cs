using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ContractClauseDto
    {
        public Guid Id { get; set; }
        public string ClauseTitle { get; set; } = string.Empty;
        public string ClauseContent { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}