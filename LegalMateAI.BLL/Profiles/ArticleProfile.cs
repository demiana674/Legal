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
    public class ArticleProfile :Profile
    {
        public ArticleProfile()
        {
            CreateMap<ArticleCreateDTO, Articles>();
            CreateMap<Articles, ArticleReadDTO>()
                .ForMember(dest => dest.LawTitle, opt => opt.MapFrom(src => src.Law.Title));

            CreateMap<ArticleUpdateDTO, Articles>();


        }
    }
}