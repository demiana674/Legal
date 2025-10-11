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
    public class NotificationProfile : Profile
    {
        public NotificationProfile() {
            CreateMap<Notification, NotificationReadDTO>();
            CreateMap<NotificationCreateDTO, Notification>();            
            CreateMap<NotificationUpdateDTO, Notification>();
        }
    }
}