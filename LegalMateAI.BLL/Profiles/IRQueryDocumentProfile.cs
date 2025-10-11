using AutoMapper;
using LegalMateAI.Domain.Entities;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.BLL.Profiles
{
    public class IRQueryDocumentProfile : Profile
    {
        public IRQueryDocumentProfile()
        {
            CreateMap<IRQueryDocument, IRQueryDocumentReadDTO>()
                .ForMember(dest => dest.QueryText, opt => opt.MapFrom(src => src.Query.QueryText))
                .ForMember(dest => dest.DocumentTitle, opt => opt.MapFrom(src => src.Document.Title));

            CreateMap<IRQueryDocumentCreateDTO, IRQueryDocument>();
        }
    }
}