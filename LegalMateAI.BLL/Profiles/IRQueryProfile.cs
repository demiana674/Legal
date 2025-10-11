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
    public class IRQueryProfile: Profile
    {
        public IRQueryProfile()
        {
            CreateMap<IRQueries, IRQueryReadDTO>();
             
            CreateMap<IRQueryCreateDTO, IRQueries>();
            CreateMap<IRQueryUpdateDTO, IRQueries>();
        }
    }
}