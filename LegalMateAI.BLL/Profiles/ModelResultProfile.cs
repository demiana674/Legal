using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using LegalMateAI.Domain.Entities;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Profiles
{
   public class ModelResultProfile: Profile
    {
        public ModelResultProfile() {
            CreateMap<ModelResults, ModelResultReadDTO>();
          

            
            CreateMap<ModelResultCreateDTO, ModelResults>()
                .ForMember(dest => dest.ProcessedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
        }
    }
}