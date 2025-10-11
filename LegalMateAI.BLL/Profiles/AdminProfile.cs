using LegalMateAI.Domain.Entities;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using AutoMapper;

namespace LegalMateAI.BLL.Profiles
{
    public class AdminProfile : Profile
    {
        public AdminProfile()
        {
            CreateMap<Admin, AdminReadDTO>();
            CreateMap<AdminCreateDTO, Admin>();
            CreateMap<AdminUpdateDTO, Admin>();
            CreateMap<AdminLog, AdminLogReadDTO>()
              .ForMember(dest => dest.AdminName, opt => opt.MapFrom(src => src.Admin.Name));

            CreateMap<AdminLogCreateDTO, AdminLog>();

        }
    }
}