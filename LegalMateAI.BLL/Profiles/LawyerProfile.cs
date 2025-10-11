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
    public class LawyerProfile: Profile
    {
        public LawyerProfile()
        {
            
            CreateMap<Lawyer, LawyerReadDto>();

           
            CreateMap<LawyerCreateDto, Lawyer>()
                .ForMember(dest => dest.JoinDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => true));

            CreateMap<LawyerUpdateDTO, Lawyer>();
        }
    }
}
