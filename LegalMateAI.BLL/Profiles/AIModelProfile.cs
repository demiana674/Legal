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
    public class AIModelProfile : Profile
    {
        public AIModelProfile()
        {
            CreateMap<AIModels, AIModelReadDTO>();
            CreateMap<AIModelCreateDTO, AIModels>();
            CreateMap<AIModelUpdateDTO, AIModels>();
        }
    }
}