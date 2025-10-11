using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using LegalMateAI.Domain.Entities;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;

namespace LegalMateAI.BLL.Profiles
{
    public class ClauseProfile : Profile
    {
        public ClauseProfile()
        {
            CreateMap<Clause, ClauseReadDTO>()
                  .ForMember(dest => dest.ArticleNumber, opt => opt.MapFrom(src => src.Article.ArticleNumber))
                .ForMember(dest => dest.LawTitle, opt => opt.MapFrom(src => src.Article.Law.Title));
            CreateMap<ClauseCreateDTO, Clause>();
            CreateMap<ClauseUpdateDTO, Clause>();
        }

    }
}