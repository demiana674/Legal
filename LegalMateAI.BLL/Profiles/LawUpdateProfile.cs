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
    public class LawUpdateProfile : Profile
    {
        public LawUpdateProfile() {
            CreateMap<LawUpdateCreateDTO, LawUpdates>();
            CreateMap<LawUpdateUpdateDTO, LawUpdates>();
            CreateMap<LawUpdates, LawUpdateReadDTO>()
                .ForMember(dest => dest.LawTitle, opt => opt.MapFrom(src => src.Law.Title));
        }
    }
}
