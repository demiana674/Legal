using LegalMateAI.Domain.Entities;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;

namespace LegalMateAI.BLL.Profiles
{
    public class IRDocumentProfile : Profile
    {
        public IRDocumentProfile() {
            CreateMap<IRDocuments, IRDocumentReadDTO>();
            CreateMap<IRDocumentCreateDTO, IRDocuments>();
            CreateMap<IRDocumentUpdateDTO, IRDocuments>();

        }
    }
}