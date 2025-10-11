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
    public class ConsultationProfile : Profile
    {
        public ConsultationProfile() {
            CreateMap<Consultation, ConsultationReadDTO>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Name))
                .ForMember(dest => dest.LawyerName, opt => opt.MapFrom(src => src.Lawyer != null ? src.Lawyer.FullName : null));
            CreateMap<ConsultationCreateDTO, Consultation>();
            CreateMap<ConsultationUpdateDTO, Consultation>();


        }

    }
}